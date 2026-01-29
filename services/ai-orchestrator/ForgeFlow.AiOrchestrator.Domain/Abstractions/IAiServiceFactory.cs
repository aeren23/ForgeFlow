using ForgeFlow.AiOrchestrator.Domain.Enums;

namespace ForgeFlow.AiOrchestrator.Domain.Abstractions;

/// <summary>
/// Factory interface for selecting the appropriate AI service.
/// Abstraction to avoid Application -> Infrastructure dependency.
/// </summary>
public interface IAiServiceFactory
{
    /// <summary>
    /// Gets the AI service based on user preference or default configuration
    /// </summary>
    /// <param name="preferredProvider">Optional: User's preferred provider</param>
    /// <returns>The appropriate AI service</returns>
    IAiService GetService(AiProviderType? preferredProvider = null);

    /// <summary>
    /// Gets all available AI services
    /// </summary>
    IEnumerable<IAiService> GetAllServices();

    /// <summary>
    /// Checks which providers are currently available (API key configured)
    /// </summary>
    Task<Dictionary<AiProviderType, bool>> GetAvailabilityStatusAsync(CancellationToken cancellationToken = default);
}
