using System.Text.Json;
using System.Text.Json.Nodes;
using ForgeFlow.Artifact.Application.Abstractions;
using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Artifact.Api.Consumers;

/// <summary>
/// GitHub Webhook → Artifact Service
/// PR merged/closed olduğunda ilgili code review artifact'ını bulup metadata'sını günceller.
/// Ardından frontend'e bildirim gitmesi için CodeReviewUpdated event'i yayınlar.
/// </summary>
public class PullRequestStatusChangedConsumer : IConsumer<PullRequestStatusChanged>
{
    private readonly IArtifactRepository _repo;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PullRequestStatusChangedConsumer> _logger;

    public PullRequestStatusChangedConsumer(
        IArtifactRepository repo,
        IPublishEndpoint publishEndpoint,
        ILogger<PullRequestStatusChangedConsumer> logger)
    {
        _repo = repo;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PullRequestStatusChanged> context)
    {
        var msg = context.Message;
        var correlationId = $"pr-{msg.PullNumber}";

        _logger.LogInformation(
            "Updating PR status for review | Issue={IssueKey} PR=#{PullNumber} Status={Status}",
            msg.IssueKey, msg.PullNumber, msg.Status);

        try
        {
            // İlgili revision'ı bul
            var revision = await _repo.FindRevisionByCorrelationIdAsync(correlationId, "CODE_REVIEW", context.CancellationToken);

            if (revision == null)
            {
                _logger.LogWarning("No code review artifact found for PR #{PullNumber} (CorrelationId={CorrelationId})",
                    msg.PullNumber, correlationId);
                return;
            }

            // Metadata güncelle
            var metadataJson = revision.Metadata ?? "{}";
            var jsonNode = JsonNode.Parse(metadataJson) ?? new JsonObject();

            // "PrStatus" alanını ekle/güncelle
            jsonNode["PrStatus"] = msg.Status;

            // Revision entity güncelle
            revision.UpdateMetadata(jsonNode.ToJsonString());

            await _repo.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Updated metadata for PR #{PullNumber} -> Status={Status}", msg.PullNumber, msg.Status);

            // Frontend'e bildirim gönder
            // ProjectId'yi revision'ın bağlı olduğu Artifact'ten (navigation prop) alıyoruz.
            // Artifact property implementation: ArtifactRevision.Artifact navigation property eklenmiş olmalı.
            var projectIdStr = revision.Artifact.ProjectId;

            // ProjectId string formatında olabilir (GUID veya key). 
            // Contract GUID bekliyor, ama Artifact.ProjectId string.
            // Eğer GUID parse edilebiliyorsa gönderelim, değilse boş GUID.
            // Not: Artifact.ProjectId aslında GUID tutuyor (daha önceki debug'da gördük).

            Guid projectIdGuid = Guid.Empty;
            if (Guid.TryParse(projectIdStr, out var parsedGuid))
            {
                projectIdGuid = parsedGuid;
            }

            await _publishEndpoint.Publish(new CodeReviewUpdated(
                IssueKey: msg.IssueKey,
                ProjectId: projectIdGuid,
                PullNumber: msg.PullNumber,
                PrStatus: msg.Status
            ));

            _logger.LogInformation("Published CodeReviewUpdated event for Issue={IssueKey}", msg.IssueKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update PR status for review | PR=#{PullNumber}", msg.PullNumber);
            // Hata fırlatma ki retry loop'a girmesin (PR status update kritik değil)
        }
    }
}
