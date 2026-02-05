using Octokit;

namespace ForgeFlow.GitHub.Infrastructure.GitHub;

/// <summary>
/// GitHub client oluşturmak için factory interface
/// </summary>
public interface IGitHubClientFactory
{
    /// <summary>
    /// Belirtilen installation için authenticated GitHubClient oluşturur
    /// </summary>
    Task<IGitHubClient> CreateClientAsync(long installationId);
}
