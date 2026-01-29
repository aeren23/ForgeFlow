using ForgeFlow.AiOrchestrator.Application.Commands;
using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior for idempotency.
/// Prevents duplicate processing and returns cached response for already-processed requests.
/// Supports at-least-once delivery guarantee.
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : GenerateAiPlanCommand
    where TResponse : GenerateAiPlanResult
{
    private readonly IAiAuditRepository _auditRepository;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IAiAuditRepository auditRepository,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if this request has already been processed successfully
        var cachedAuditLog = await _auditRepository.GetByRequestIdAsync(
            request.RequestId, 
            cancellationToken);

        if (cachedAuditLog != null)
        {
            _logger.LogInformation(
                "IdempotencyBehavior: Request {RequestId} already processed. Returning cached response.",
                request.RequestId);

            // Return the cached response (supports at-least-once delivery)
            var cachedResult = new GenerateAiPlanResult
            {
                IsSuccess = true,
                GeneratedPlan = cachedAuditLog.GeneratedContent,
                UsedProvider = cachedAuditLog.Provider,
                ModelName = cachedAuditLog.ModelName,
                PromptTokens = cachedAuditLog.PromptTokens,
                CompletionTokens = cachedAuditLog.CompletionTokens,
                DurationMs = cachedAuditLog.DurationMs,
                ErrorCode = "CACHED_RESPONSE" // Marker to indicate this is from cache
            };

            return (TResponse)(object)cachedResult;
        }

        _logger.LogDebug("IdempotencyBehavior: Request {RequestId} is new, proceeding.", request.RequestId);

        // Proceed with the actual handler
        return await next();
    }
}
