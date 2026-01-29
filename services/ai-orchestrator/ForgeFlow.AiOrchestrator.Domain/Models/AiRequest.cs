namespace ForgeFlow.AiOrchestrator.Domain.Models;

/// <summary>
/// Request model for AI content generation
/// </summary>
public class AiRequest
{
    /// <summary>
    /// Unique request ID for idempotency checks
    /// </summary>
    public Guid RequestId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// System prompt defining AI behavior and role
    /// </summary>
    public required string SystemPrompt { get; set; }

    /// <summary>
    /// User prompt containing the actual task/question
    /// </summary>
    public required string UserPrompt { get; set; }

    /// <summary>
    /// Maximum tokens to generate (default: 4096)
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for response creativity (0.0 = deterministic, 1.0 = creative)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Optional: Preferred provider (null = use default)
    /// </summary>
    public Enums.AiProviderType? PreferredProvider { get; set; }
}
