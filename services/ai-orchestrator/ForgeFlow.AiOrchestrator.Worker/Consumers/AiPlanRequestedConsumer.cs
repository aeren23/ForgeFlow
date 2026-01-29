using ForgeFlow.AiOrchestrator.Application.Commands;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Worker.Consumers;

/// <summary>
/// MassTransit consumer for AiPlanRequested events.
/// Delegates to MediatR for orchestration.
/// </summary>
public class AiPlanRequestedConsumer : IConsumer<EventEnvelope<AiPlanRequested>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AiPlanRequestedConsumer> _logger;

    public AiPlanRequestedConsumer(IMediator mediator, ILogger<AiPlanRequestedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<AiPlanRequested>> context)
    {
        var msg = context.Message;

        // Structured logging scope - Seq'te CorrelationId ile tüm akışı izleyebiliriz
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = msg.CorrelationId,
            ["EventId"] = msg.EventId,
            ["UserId"] = msg.UserId,
            ["IssueId"] = msg.Data.IssueId
        });

        _logger.LogInformation(
            "AI Orchestrator received AiPlanRequested | IssueId={IssueId} BundleType={BundleType}",
            msg.Data.IssueId,
            msg.Data.BundleType);

        try
        {
            // Parse CorrelationId from string to Guid
            // If parse fails, use EventId to maintain traceability (not a random GUID!)
            var correlationId = Guid.TryParse(msg.CorrelationId, out var cid) ? cid : msg.EventId;

            // Create MediatR command from event
            // PreferredProvider is passed as string, Handler will parse to enum
            var command = new GenerateAiPlanCommand
            {
                RequestId = msg.EventId, // Use EventId for idempotency
                CorrelationId = correlationId,
                ProjectId = Guid.TryParse(msg.Data.ProjectId, out var pid) ? pid : Guid.Empty,
                IssueKey = msg.Data.IssueId,
                UserId = msg.UserId,
                BundleType = msg.Data.BundleType,
                Strictness = msg.Data.Strictness,
                PreferredProvider = msg.Data.PreferredProvider // String, parsed in Handler
            };

            // Execute via MediatR (triggers Idempotency + AuditLogging behaviors)
            var result = await _mediator.Send(command, context.CancellationToken);

            if (result.IsSuccess)
            {
                // Publish success event
                var successEvent = new EventEnvelope<AiPlanGenerated>(
                    EventId: Guid.NewGuid(),
                    OccurredAtUtc: DateTime.UtcNow,
                    CorrelationId: msg.CorrelationId,
                    UserId: msg.UserId,
                    CausationId: msg.EventId.ToString(),
                    Data: new AiPlanGenerated(
                        IssueId: msg.Data.IssueId,
                        ProjectId: msg.Data.ProjectId,
                        ArtifactType: msg.Data.BundleType,
                        GeneratedContent: result.GeneratedPlan ?? "",
                        UsedProvider: result.UsedProvider.ToString(),
                        PromptTokens: result.PromptTokens,
                        CompletionTokens: result.CompletionTokens,
                        DurationMs: result.DurationMs
                    )
                );

                await context.Publish(successEvent);

                _logger.LogInformation(
                    "AI Plan generated successfully | IssueId={IssueId} Provider={Provider} Tokens={Tokens}",
                    msg.Data.IssueId,
                    result.UsedProvider,
                    result.PromptTokens + result.CompletionTokens);
            }
            else
            {
                // Publish failure event
                var failureEvent = new EventEnvelope<AiPlanFailed>(
                    EventId: Guid.NewGuid(),
                    OccurredAtUtc: DateTime.UtcNow,
                    CorrelationId: msg.CorrelationId,
                    UserId: msg.UserId,
                    CausationId: msg.EventId.ToString(),
                    Data: new AiPlanFailed(
                        IssueId: msg.Data.IssueId,
                        ProjectId: msg.Data.ProjectId,
                        ErrorCode: result.ErrorCode ?? "UNKNOWN",
                        ErrorMessage: result.ErrorMessage ?? "Unknown error",
                        UsedProvider: result.UsedProvider.ToString()
                    )
                );

                await context.Publish(failureEvent);

                _logger.LogWarning(
                    "AI Plan generation failed | IssueId={IssueId} Error={ErrorCode}: {ErrorMessage}",
                    msg.Data.IssueId,
                    result.ErrorCode,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception in AI Plan generation | IssueId={IssueId}",
                msg.Data.IssueId);

            // Re-throw to let MassTransit retry mechanism handle it
            throw;
        }
    }
}
