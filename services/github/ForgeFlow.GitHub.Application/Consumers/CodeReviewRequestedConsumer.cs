using System.Net.Http;
using System.Text;
using ForgeFlow.Contracts.Events;
using ForgeFlow.GitHub.Application.Services;
using ForgeFlow.GitHub.Infrastructure.GitHub;
using ForgeFlow.GitHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace ForgeFlow.GitHub.Application.Consumers;

/// <summary>
/// CodeReviewRequested event'ini tüketir.
/// 1. GitHub'dan PR diff'ini çeker
/// 2. Artifact Service'ten orijinal planı çeker (HTTP)
/// 3. CodeReviewWithDiffReady event'i publish eder → AI Orchestrator tüketir
/// </summary>
public class CodeReviewRequestedConsumer : IConsumer<CodeReviewRequested>
{
    private readonly IGitHubClientFactory _clientFactory;
    private readonly IPullRequestService _prService;
    private readonly GitHubDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CodeReviewRequestedConsumer> _logger;

    public CodeReviewRequestedConsumer(
        IGitHubClientFactory clientFactory,
        IPullRequestService prService,
        GitHubDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<CodeReviewRequestedConsumer> logger)
    {
        _clientFactory = clientFactory;
        _prService = prService;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CodeReviewRequested> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Fetching PR diff for code review | Issue={IssueKey} PR=#{PullNumber} Repo={Owner}/{Repo}",
            msg.IssueKey, msg.PullNumber, msg.RepositoryOwner, msg.RepositoryName);

        try
        {
            // 0. ProjectId resolve et (WebhookController DB erişimi olmadığı için boş gönderir)
            var projectId = msg.ProjectId;
            if (string.IsNullOrEmpty(projectId))
            {
                var fullName = $"{msg.RepositoryOwner}/{msg.RepositoryName}";
                var repoConnection = await _db.RepositoryConnections
                    .FirstOrDefaultAsync(r => r.FullName.ToLower() == fullName.ToLower());

                if (repoConnection != null)
                {
                    projectId = repoConnection.ProjectId.ToString();
                    _logger.LogInformation("Resolved ProjectId={ProjectId} from repo {FullName}", projectId, fullName);
                }
                else
                {
                    _logger.LogWarning("No RepositoryConnection found for {FullName}, using empty ProjectId", fullName);
                }
            }

            // 1. GitHub client oluştur
            var client = await _clientFactory.CreateClientAsync(msg.InstallationId);

            // 2. PR diff'ini çek
            var diff = await _prService.GetPullRequestDiffAsync(
                client, msg.RepositoryOwner, msg.RepositoryName, msg.PullNumber);

            // 3. Diff'i string olarak formatla
            var diffContent = FormatDiffForAi(diff);

            // 4. Orijinal planı çekmeye çalış (Artifact Service)
            string? originalPlan = await FetchOriginalPlanAsync(msg.IssueKey, projectId);

            // 5. Zenginleştirilmiş event publish et → AI Orchestrator tüketecek
            await context.Publish(new CodeReviewWithDiffReady(
                IssueKey: msg.IssueKey,
                ProjectId: projectId,
                PullNumber: msg.PullNumber,
                PrTitle: diff.PrTitle,
                PrDescription: diff.PrBody,
                DiffContent: diffContent,
                OriginalPlanJson: originalPlan,
                Timestamp: DateTime.UtcNow
            ));

            _logger.LogInformation(
                "Published CodeReviewWithDiffReady | Issue={IssueKey} PR=#{PullNumber} Files={FileCount} +{Add}/-{Del}",
                msg.IssueKey, msg.PullNumber, diff.ChangedFiles, diff.Additions, diff.Deletions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch PR diff for code review | Issue={IssueKey} PR=#{PullNumber}",
                msg.IssueKey, msg.PullNumber);

            // Review başarısız olsa bile issue flow'u etkilemesin
            await context.Publish(new CodeReviewFailed(
                IssueKey: msg.IssueKey,
                ProjectId: msg.ProjectId,
                PullNumber: msg.PullNumber,
                ErrorMessage: $"Failed to fetch PR diff: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// PR diff'ini AI'ın anlayabileceği formatta birleştirir
    /// </summary>
    private static string FormatDiffForAi(PrDiffResult diff)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## PR: {diff.PrTitle}");
        sb.AppendLine($"Files changed: {diff.ChangedFiles} | +{diff.Additions} -{diff.Deletions}");
        sb.AppendLine();

        foreach (var file in diff.Files)
        {
            sb.AppendLine($"### {file.Status.ToUpper()}: {file.FileName} (+{file.Additions}/-{file.Deletions})");
            if (!string.IsNullOrEmpty(file.Patch))
            {
                sb.AppendLine("```diff");
                sb.AppendLine(file.Patch);
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Artifact Service'ten orijinal AI planını çeker (HTTP call)
    /// </summary>
    private async Task<string?> FetchOriginalPlanAsync(string issueKey, string projectId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ArtifactService");
            var response = await httpClient.GetAsync(
                $"/api/artifacts/latest?issueId={issueKey}&type=BUNDLE_V1&projectId={projectId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Fetched original plan for issue {IssueKey}", issueKey);
                return content;
            }

            _logger.LogDebug("No original plan found for issue {IssueKey}", issueKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch original plan for issue {IssueKey}, proceeding without it", issueKey);
            return null;
        }
    }
}
