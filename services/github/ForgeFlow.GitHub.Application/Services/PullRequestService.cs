using Microsoft.Extensions.Logging;
using Octokit;

namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// PR diff çekme ve review comment yazma implementasyonu
/// </summary>
public class PullRequestService : IPullRequestService
{
    private readonly ILogger<PullRequestService> _logger;

    public PullRequestService(ILogger<PullRequestService> logger)
    {
        _logger = logger;
    }

    public async Task<PrDiffResult> GetPullRequestDiffAsync(
        IGitHubClient client, string owner, string repo, int prNumber)
    {
        _logger.LogInformation("Fetching PR diff: {Owner}/{Repo}#{PrNumber}", owner, repo, prNumber);

        // PR bilgilerini çek
        var pr = await client.PullRequest.Get(owner, repo, prNumber);

        // Değişen dosyaları çek
        var files = await client.PullRequest.Files(owner, repo, prNumber);

        var fileChanges = files.Select(f => new PrFileChange(
            FileName: f.FileName,
            Status: f.Status,
            Additions: f.Additions,
            Deletions: f.Deletions,
            Patch: f.Patch
        )).ToList();

        _logger.LogInformation(
            "PR #{PrNumber} diff: {FileCount} files, +{Additions}/-{Deletions}",
            prNumber, fileChanges.Count, pr.Additions, pr.Deletions);

        return new PrDiffResult(
            PrTitle: pr.Title,
            PrBody: pr.Body ?? "",
            Additions: pr.Additions,
            Deletions: pr.Deletions,
            ChangedFiles: pr.ChangedFiles,
            Files: fileChanges
        );
    }

    public async Task WriteReviewCommentAsync(
        IGitHubClient client, string owner, string repo,
        int prNumber, string reviewBody)
    {
        _logger.LogInformation("Writing review comment to PR #{PrNumber} in {Owner}/{Repo}",
            prNumber, owner, repo);

        // PR'a review olarak gönder (COMMENT tipi — approve/reject değil)
        var review = new PullRequestReviewCreate
        {
            Body = reviewBody,
            Event = PullRequestReviewEvent.Comment
        };

        await client.PullRequest.Review.Create(owner, repo, prNumber, review);

        _logger.LogInformation("Review comment written to PR #{PrNumber}", prNumber);
    }
}
