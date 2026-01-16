using ForgeFlow.Artifact.Application.Artifacts.Commands;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Artifact.Api.Consumers;

public class ArtifactGeneratedConsumer : IConsumer<EventEnvelope<ArtifactGenerated>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ArtifactGeneratedConsumer> _logger;

    public ArtifactGeneratedConsumer(IMediator mediator, ILogger<ArtifactGeneratedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<ArtifactGenerated>> context)
    {
        var msg = context.Message;

        // Seq üzerinde CorrelationId ile tüm akışı izleyebiliriz
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = msg.CorrelationId,
            ["EventId"] = msg.EventId
        });

        _logger.LogInformation(
            "Processing ArtifactGenerated event | IssueId={IssueId} Type={Type}",
            msg.Data.IssueId, msg.Data.ArtifactType);

        var contentJson = $$"""
        {
          "type": "{{msg.Data.ArtifactType}}",
          "issueId": "{{msg.Data.IssueId}}",
          "generatedAtUtc": "{{DateTime.UtcNow:O}}",
          "note": "Placeholder content - AI output will be stored here later"
        }
        """;

        var rev = await _mediator.Send(new UpsertArtifactRevisionCommand(
            ProjectId: msg.Data.ProjectId,
            IssueId: msg.Data.IssueId,
            Type: msg.Data.ArtifactType,
            ContentJson: contentJson,
            CorrelationId: msg.CorrelationId
        ), context.CancellationToken);

        if (rev == -1) // Idempotency flag - duplicate event
        {
            _logger.LogInformation(
                "Skipping: Artifact revision already exists for CorrelationId={CorrelationId}",
                msg.CorrelationId);
            return;
        }

        _logger.LogInformation(
            "Saved Artifact revision | IssueId={IssueId} Type={Type} Rev={Rev}",
            msg.Data.IssueId, msg.Data.ArtifactType, rev);
    }
}
