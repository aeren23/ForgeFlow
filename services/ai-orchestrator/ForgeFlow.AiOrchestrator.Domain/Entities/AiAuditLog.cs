using ForgeFlow.AiOrchestrator.Domain.Enums;

namespace ForgeFlow.AiOrchestrator.Domain.Entities;

/// <summary>
/// Audit log entity for tracking all AI/LLM interactions.
/// Provides traceability for debugging, cost tracking, and compliance.
/// </summary>
public class AiAuditLog
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Correlation ID for request tracing across services
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Original request ID (for idempotency tracking)
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Associated issue key (e.g., "FORGE-1")
    /// </summary>
    public required string IssueKey { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// User who initiated the request
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// AI provider used
    /// </summary>
    public AiProviderType Provider { get; set; }

    /// <summary>
    /// Model name (e.g., "gemini-1.5-pro")
    /// </summary>
    public required string ModelName { get; set; }

    /// <summary>
    /// System prompt sent to AI
    /// </summary>
    public required string SystemPrompt { get; set; }

    /// <summary>
    /// User prompt sent to AI
    /// </summary>
    public required string UserPrompt { get; set; }

    /// <summary>
    /// Generated content (may be truncated for large responses)
    /// </summary>
    public string? GeneratedContent { get; set; }

    /// <summary>
    /// Tokens used in the prompt
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Tokens generated in the response
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the request was made
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
