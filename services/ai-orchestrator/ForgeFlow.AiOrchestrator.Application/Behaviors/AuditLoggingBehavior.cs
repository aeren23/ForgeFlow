using System.Diagnostics;
using ForgeFlow.AiOrchestrator.Application.Commands;
using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Entities;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior for automatic audit logging of AI operations.
/// Logs every AI request with prompts, responses, token usage, and timing.
/// </summary>
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : GenerateAiPlanCommand
    where TResponse : GenerateAiPlanResult
{
    private readonly IAiAuditRepository _auditRepository;
    private readonly ILogger<AuditLoggingBehavior<TRequest, TResponse>> _logger;

    public AuditLoggingBehavior(
        IAiAuditRepository auditRepository,
        ILogger<AuditLoggingBehavior<TRequest, TResponse>> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("AuditLoggingBehavior: Starting for RequestId={RequestId}", request.RequestId);

        // Execute the actual handler
        var response = await next();

        stopwatch.Stop();

        // Create audit log entry
        var auditLog = new AiAuditLog
        {
            CorrelationId = request.CorrelationId,
            RequestId = request.RequestId,
            IssueKey = request.IssueKey,
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            Provider = response.UsedProvider,
            ModelName = response.ModelName ?? "unknown",
            SystemPrompt = $"[BundleType: {request.BundleType}, Strictness: {request.Strictness}]",
            UserPrompt = $"Issue: {request.IssueKey}",
            GeneratedContent = response.GeneratedPlan?[..Math.Min(response.GeneratedPlan.Length, 5000)], // Truncate for storage
            PromptTokens = response.PromptTokens,
            CompletionTokens = response.CompletionTokens,
            DurationMs = stopwatch.ElapsedMilliseconds,
            IsSuccess = response.IsSuccess,
            ErrorCode = response.ErrorCode,
            ErrorMessage = response.ErrorMessage
        };

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AuditLog saved: RequestId={RequestId}, Success={Success}, Duration={Duration}ms, Tokens={Tokens}",
            request.RequestId,
            response.IsSuccess,
            stopwatch.ElapsedMilliseconds,
            response.PromptTokens + response.CompletionTokens);

        return response;
    }
}
