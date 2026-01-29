using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ForgeFlow.AiOrchestrator.Infrastructure.AiServices;

/// <summary>
/// Factory for selecting the appropriate AI service based on configuration or user preference
/// </summary>
public class AiServiceFactory : IAiServiceFactory
{
    private readonly IEnumerable<IAiService> _services;
    private readonly AiOptions _options;


    public AiServiceFactory(
        IEnumerable<IAiService> services,
        IOptions<AiOptions> options)
    {
        _services = services;
        _options = options.Value;
    }

    /// <summary>
    /// Gets the AI service based on user preference or default configuration
    /// </summary>
    /// <param name="preferredProvider">Optional: User's preferred provider</param>
    /// <returns>The appropriate AI service</returns>
    public IAiService GetService(AiProviderType? preferredProvider = null)
    {
        // If user specified a preference, try to use that
        if (preferredProvider.HasValue)
        {
            var preferredService = _services.FirstOrDefault(s => s.ProviderType == preferredProvider.Value);
            if (preferredService != null)
                return preferredService;
        }

        // Use default provider from configuration
        var defaultProviderType = Enum.Parse<AiProviderType>(_options.DefaultProvider, ignoreCase: true);
        var defaultService = _services.FirstOrDefault(s => s.ProviderType == defaultProviderType);

        if (defaultService != null)
            return defaultService;

        // Fallback to first available service
        return _services.First();
    }

    /// <summary>
    /// Gets all available AI services
    /// </summary>
    public IEnumerable<IAiService> GetAllServices() => _services;

    /// <summary>
    /// Checks which providers are currently available (API key configured)
    /// </summary>
    public async Task<Dictionary<AiProviderType, bool>> GetAvailabilityStatusAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<AiProviderType, bool>();

        foreach (var service in _services)
        {
            result[service.ProviderType] = await service.IsAvailableAsync(cancellationToken);
        }

        return result;
    }
}
