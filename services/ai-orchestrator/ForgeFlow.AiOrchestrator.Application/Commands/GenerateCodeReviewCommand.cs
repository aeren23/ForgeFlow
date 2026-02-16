using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using MediatR;

namespace ForgeFlow.AiOrchestrator.Application.Commands;

/// <summary>
/// Command to generate an AI code review for a PR diff.
/// Follows same pattern as GenerateAiPlanCommand.
/// </summary>
public class GenerateCodeReviewCommand : IRequest<GenerateCodeReviewResult>
{
    public Guid RequestId { get; init; }
    public Guid CorrelationId { get; init; }
    public required string ProjectId { get; init; }
    public required string IssueKey { get; init; }
    public int PullNumber { get; init; }

    /// <summary>
    /// PR diff içeriği (unified diff format)
    /// </summary>
    public required string DiffContent { get; init; }

    /// <summary>
    /// Artifact Service'ten çekilen orijinal AI planı (plan uygunluk karşılaştırması için)
    /// </summary>
    public string? OriginalPlanJson { get; init; }

    /// <summary>
    /// PR başlığı ve açıklaması — ek context sağlar
    /// </summary>
    public string? PrTitle { get; init; }
    public string? PrDescription { get; init; }

    /// <summary>
    /// Kullanılacak AI provider (null = default)
    /// </summary>
    public string? PreferredProvider { get; init; }
}

/// <summary>
/// AI code review sonucu
/// </summary>
public class GenerateCodeReviewResult
{
    public bool IsSuccess { get; init; }
    public string? ReviewContent { get; init; }  // JSON review
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    // Metrics
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public long DurationMs { get; init; }
    public AiProviderType UsedProvider { get; init; }
    public string? ModelName { get; init; }

    public static GenerateCodeReviewResult Success(AiResponse response) => new()
    {
        IsSuccess = true,
        ReviewContent = response.Content,
        PromptTokens = response.PromptTokens,
        CompletionTokens = response.CompletionTokens,
        DurationMs = response.DurationMs,
        UsedProvider = response.Provider,
        ModelName = response.ModelName
    };

    public static GenerateCodeReviewResult Failure(string errorMessage, string errorCode, AiProviderType provider) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode,
        UsedProvider = provider
    };
}
