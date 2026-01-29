namespace ForgeFlow.AiOrchestrator.Domain.Models;

/// <summary>
/// Response model from AI content generation with usage metrics
/// </summary>
public class AiResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Generated content (plan, code, etc.)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for categorization (e.g., "QUOTA_EXCEEDED", "SERVICE_UNAVAILABLE")
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Provider that handled the request
    /// </summary>
    public Enums.AiProviderType Provider { get; set; }

    /// <summary>
    /// Model name used (e.g., "gemini-1.5-pro")
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Tokens used in the prompt
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Tokens generated in the response
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used (prompt + completion)
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Remaining requests before rate limit (from API headers)
    /// </summary>
    public int? RemainingRequests { get; set; }

    /// <summary>
    /// Remaining tokens before rate limit (from API headers)
    /// </summary>
    public int? RemainingTokens { get; set; }

    /// <summary>
    /// When the rate limit resets
    /// </summary>
    public DateTime? RateLimitResetTime { get; set; }

    /// <summary>
    /// Factory method for successful response
    /// </summary>
    public static AiResponse Success(string content, Enums.AiProviderType provider, string modelName)
        => new()
        {
            IsSuccess = true,
            Content = content,
            Provider = provider,
            ModelName = modelName
        };

    /// <summary>
    /// Factory method for failed response
    /// </summary>
    public static AiResponse Failure(string errorMessage, string errorCode, Enums.AiProviderType provider)
        => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Provider = provider
        };
}
