using ForgeFlow.AiOrchestrator.Domain.Models;

namespace ForgeFlow.AiOrchestrator.Domain.Abstractions;

/// <summary>
/// Abstraction for context providers.
/// Fetches project/issue information from various sources (Database, GitHub, etc.)
/// 
/// GitHub-ready: When GitHub integration is added, a GitHubContextProvider will implement this.
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Provider source name (e.g., "Database", "GitHub", "Composite")
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Priority order when multiple providers are available (lower = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gathers all available context for AI plan generation
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="issueKey">The issue key (e.g., "FORGE-1")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated context for AI consumption</returns>
    Task<AiContext> GetContextAsync(Guid projectId, string issueKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the context provider is available and can fetch data
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
