using Octokit;

namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// PR diff çekme ve PR'a review comment yazma servisi
/// </summary>
public interface IPullRequestService
{
    /// <summary>
    /// PR'daki değişen dosyaları ve patch'leri çeker
    /// </summary>
    Task<PrDiffResult> GetPullRequestDiffAsync(
        IGitHubClient client, string owner, string repo, int prNumber);

    /// <summary>
    /// PR'a review comment yazar
    /// </summary>
    Task WriteReviewCommentAsync(
        IGitHubClient client, string owner, string repo,
        int prNumber, string reviewBody);
}

/// <summary>
/// PR diff sonucu
/// </summary>
public record PrDiffResult(
    string PrTitle,
    string PrBody,
    int Additions,
    int Deletions,
    int ChangedFiles,
    List<PrFileChange> Files
);

/// <summary>
/// Değişen tek dosya bilgisi
/// </summary>
public record PrFileChange(
    string FileName,
    string Status,       // added, modified, removed, renamed
    int Additions,
    int Deletions,
    string? Patch        // Unified diff patch
);
