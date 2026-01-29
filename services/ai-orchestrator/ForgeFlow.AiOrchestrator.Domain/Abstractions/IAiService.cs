using ForgeFlow.AiOrchestrator.Domain.Models;
using ForgeFlow.AiOrchestrator.Domain.Enums;

namespace ForgeFlow.AiOrchestrator.Domain.Abstractions;

/// <summary>
/// Abstraction for AI/LLM service providers.
/// Implementations: GeminiAiService, GroqAiService, OpenAiService
/// </summary>
public interface IAiService
{
    /// <summary>
    /// The provider type this service represents
    /// </summary>
    AiProviderType ProviderType { get; }

    /// <summary>
    /// Display name for the model (e.g., "gemini-1.5-pro", "llama3-70b")
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Generates content (plan/code) based on the given request
    /// </summary>
    /// <param name="request">The AI request containing prompts and parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with content and usage metrics</returns>
    Task<AiResponse> GenerateContentAsync(AiRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the service is available (API key configured, service reachable)
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
