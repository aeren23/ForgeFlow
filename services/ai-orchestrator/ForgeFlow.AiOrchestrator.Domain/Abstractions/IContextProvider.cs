using ForgeFlow.AiOrchestrator.Domain.Models;

namespace ForgeFlow.AiOrchestrator.Domain.Abstractions;

/// <summary>
/// Abstraction for context providers.
/// Fetches project/issue information from various sources (Database, GitHub, etc.)
/// 
/// Supports Multi-Turn AI: Provider can fetch specific files on demand
/// when AI requests them during the discovery phase.
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Provider source name (e.g., "WorkService", "Database", "GitHub")
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Priority order when multiple providers are available (lower = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gathers all available context for AI plan generation.
    /// Includes code context (file tree + critical files) if GitHub connection is available.
    /// </summary>
    Task<AiContext> GetContextAsync(Guid projectId, string issueKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Multi-Turn AI: AI'ın istediği belirli dosyaları GitHub Service'den çeker.
    /// Discovery phase'de AI dosya ağacını görür ve okumak istediği dosyaları belirler,
    /// bu method ile o dosyalar çekilir.
    /// </summary>
    /// <param name="projectId">Proje ID</param>
    /// <param name="filePaths">AI'ın okumak istediği dosya yolları (max 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dosya yolu → içerik dictionary'si</returns>
    Task<Dictionary<string, string>> FetchRequestedFilesAsync(
        Guid projectId, string[] filePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the context provider is available and can fetch data
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

