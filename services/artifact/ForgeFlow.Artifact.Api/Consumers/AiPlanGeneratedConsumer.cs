using ForgeFlow.Artifact.Application.Artifacts.Commands;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Artifact.Api.Consumers;

public class AiPlanGeneratedConsumer : IConsumer<EventEnvelope<AiPlanGenerated>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AiPlanGeneratedConsumer> _logger;

    public AiPlanGeneratedConsumer(IMediator mediator, ILogger<AiPlanGeneratedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<AiPlanGenerated>> context)
    {
        var msg = context.Message;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = msg.CorrelationId,
            ["IssueId"] = msg.Data.IssueId,
            ["UserId"] = msg.UserId
        });

        _logger.LogInformation(
            "Artifact Service received AiPlanGenerated | IssueId={IssueId} Type={Type}",
            msg.Data.IssueId, msg.Data.ArtifactType);

        // AI Plan'ı Artifact olarak kaydet
        // Type olarak "ImplementationPlan" kullanıyoruz.
        // Gelecekte farklı artifact tipleri (TestPlan, CodePr vb.) eklenebilir.

        // Metadata oluştur: Orijinal tip ve diğer detayları sakla
        var metadata = new Dictionary<string, object>
        {
            ["OriginalType"] = msg.Data.ArtifactType ?? "Unknown",
            ["AiProvider"] = msg.Data.UsedProvider,
            ["PromptTokens"] = msg.Data.PromptTokens,
            ["CompletionTokens"] = msg.Data.CompletionTokens,
            ["DurationMs"] = msg.Data.DurationMs,
            ["GeneratedAt"] = DateTime.UtcNow
        };

        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);

        var command = new UpsertArtifactRevisionCommand(
            ProjectId: msg.Data.ProjectId,
            IssueId: msg.Data.IssueId,
            Type: "ImplementationPlan",
            ContentJson: msg.Data.GeneratedContent,
            CorrelationId: msg.CorrelationId,
            UserId: msg.UserId,
            Metadata: metadataJson
        );

        var rev = await _mediator.Send(command, context.CancellationToken);

        if (rev == -1)
        {
            _logger.LogInformation("Artifact revision already exists (Idempotent).");
            return;
        }

        _logger.LogInformation(
            "AI Plan saved as Artifact | Revision={Rev} Type=ImplementationPlan",
            rev);

        // İstersek burada bir de ArtifactGenerated event'i fırlatabiliriz ama şimdilik gerek yok.
    }
}
