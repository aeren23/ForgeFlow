using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using MediatR;

namespace ForgeFlow.AiOrchestrator.Application.Commands;

/// <summary>
/// Command to generate an AI plan for a given issue.
/// Implements CQRS pattern with MediatR.
/// </summary>
public class GenerateAiPlanCommand : IRequest<GenerateAiPlanResult>
{
    /// <summary>
    /// Unique request ID for idempotency
    /// </summary>
    public Guid RequestId { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; init; }

    /// <summary>
    /// Issue key (e.g., "FORGE-1")
    /// </summary>
    public required string IssueKey { get; init; }

    /// <summary>
    /// User ID who requested the plan
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Bundle type (e.g., "FullStack", "Backend", "Frontend")
    /// </summary>
    public string BundleType { get; init; } = "FullStack";

    /// <summary>
    /// Strictness level for AI generation
    /// </summary>
    public string Strictness { get; init; } = "Normal";

    /// <summary>
    /// Optional: User's preferred AI provider as string ("Gemini", "Groq", etc.)
    /// Parsed to AiProviderType in Handler. Null/empty = use default from config.
    /// This is string to avoid Worker layer depending on Domain types.
    /// </summary>
    public string? PreferredProvider { get; init; }
}

/// <summary>
/// Result of AI plan generation
/// </summary>
public class GenerateAiPlanResult
{
    public bool IsSuccess { get; init; }
    public string? GeneratedPlan { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    
    // Token usage metrics
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public long DurationMs { get; init; }
    public AiProviderType UsedProvider { get; init; }
    public string? ModelName { get; init; }

    // Rate limit info (for user feedback)
    public int? RemainingRequests { get; init; }
    public int? RemainingTokens { get; init; }

    public static GenerateAiPlanResult Success(AiResponse response) => new()
    {
        IsSuccess = true,
        GeneratedPlan = response.Content,
        PromptTokens = response.PromptTokens,
        CompletionTokens = response.CompletionTokens,
        DurationMs = response.DurationMs,
        UsedProvider = response.Provider,
        ModelName = response.ModelName,
        RemainingRequests = response.RemainingRequests,
        RemainingTokens = response.RemainingTokens
    };

    public static GenerateAiPlanResult Failure(string errorMessage, string errorCode, AiProviderType provider) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode,
        UsedProvider = provider
    };

    public static GenerateAiPlanResult AlreadyProcessed() => new()
    {
        IsSuccess = true,
        ErrorCode = "ALREADY_PROCESSED",
        ErrorMessage = "This request has already been processed (idempotency check)"
    };
}
