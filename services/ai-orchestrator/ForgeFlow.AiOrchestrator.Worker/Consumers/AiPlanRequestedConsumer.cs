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

        // Structured logging scope - Seq'te CorrelationId ile tüm akışı izleyebiliriz
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = msg.CorrelationId,
            ["EventId"] = msg.EventId,
            ["UserId"] = msg.UserId
        });

        _logger.LogInformation(
            "AI Orchestrator processing AiPlanRequested | IssueId={IssueId} UserId={UserId}",
            msg.Data.IssueId,
            msg.UserId
        );

        // AI yok şimdilik: direkt ArtifactGenerated üret
        // İleride burada gerçek AI çağrısı yapılacak
        var outEvt = new EventEnvelope<ArtifactGenerated>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: msg.CorrelationId,  // Aynı correlation'ı taşı
            UserId: msg.UserId,                 // UserId'yi aktar (audit için)
            CausationId: msg.EventId.ToString(),
            Data: new ArtifactGenerated(
                IssueId: msg.Data.IssueId,
                ProjectId: msg.Data.ProjectId,
                ArtifactType: msg.Data.BundleType,
                Revision: 1,
                UserId: msg.UserId              // ArtifactGenerated'a da UserId
            )
        );

        await context.Publish(outEvt);

        _logger.LogInformation(
            "Published ArtifactGenerated | IssueId={IssueId} Type={Type}",
            msg.Data.IssueId, msg.Data.BundleType);
    }
}

