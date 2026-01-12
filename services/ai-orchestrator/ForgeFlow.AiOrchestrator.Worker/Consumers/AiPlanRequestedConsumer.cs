using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Worker.Consumers;

public class AiPlanRequestedConsumer(ILogger<AiPlanRequestedConsumer> logger) : IConsumer<EventEnvelope<AiPlanRequested>>
{
    private readonly ILogger<AiPlanRequestedConsumer> _logger = logger;

    public async Task Consume(ConsumeContext<EventEnvelope<AiPlanRequested>> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "AI Orchestrator consumed AiPlanRequested | CorrelationId={CorrelationId} IssueId={IssueId}",
            msg.CorrelationId,
            msg.Data.IssueId
        );

        // AI yok şimdilik: direkt ArtifactGenerated üret
        var outEvt = new EventEnvelope<ArtifactGenerated>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: msg.CorrelationId,
            CausationId: msg.EventId.ToString(),
            Data: new ArtifactGenerated(
                IssueId: msg.Data.IssueId,
                ProjectId: msg.Data.ProjectId,
                ArtifactType: msg.Data.BundleType,
                Revision: 1
            )
        );

        await context.Publish(outEvt);
    }
}
