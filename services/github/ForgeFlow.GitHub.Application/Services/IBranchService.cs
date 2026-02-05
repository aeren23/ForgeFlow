using Octokit;

namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// Branch oluşturma servisi
/// </summary>
public interface IBranchService
{
    /// <summary>
    /// Feature branch oluşturur. develop varsa develop'tan, yoksa main'den türetir.
    /// </summary>
    Task<string> CreateFeatureBranchAsync(
        IGitHubClient client,
        string owner,
        string repo,
        string issueKey,
        string issueTitle,
        string? defaultBranch);
}
