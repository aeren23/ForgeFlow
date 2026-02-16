using ForgeFlow.Contracts.Events;
using ForgeFlow.GitHub.Application.Services;
using ForgeFlow.GitHub.Infrastructure.GitHub;
using ForgeFlow.GitHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.GitHub.Application.Consumers;

/// <summary>
/// CodeReviewCompleted event'ini tüketir.
/// AI tarafından üretilen review'ı GitHub PR'a yorum olarak yazar.
/// </summary>
public class CodeReviewCompletedConsumer : IConsumer<CodeReviewCompleted>
{
    private readonly IGitHubClientFactory _clientFactory;
    private readonly IPullRequestService _prService;
    private readonly GitHubDbContext _db;
    private readonly ILogger<CodeReviewCompletedConsumer> _logger;

    public CodeReviewCompletedConsumer(
        IGitHubClientFactory clientFactory,
        IPullRequestService prService,
        GitHubDbContext db,
        ILogger<CodeReviewCompletedConsumer> logger)
    {
        _clientFactory = clientFactory;
        _prService = prService;
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CodeReviewCompleted> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Writing AI review to GitHub PR | Issue={IssueKey} PR=#{PullNumber}",
            msg.IssueKey, msg.PullNumber);

        try
        {
            // ProjectId üzerinden repo bağlantısını bul
            var repoConnection = await _db.RepositoryConnections
                .Include(r => r.Installation)
                .FirstOrDefaultAsync(r => r.ProjectId.ToString() == msg.ProjectId);

            if (repoConnection == null)
            {
                _logger.LogWarning(
                    "No repository connection found for project {ProjectId}, cannot write PR review",
                    msg.ProjectId);
                return;
            }

            var client = await _clientFactory.CreateClientAsync(repoConnection.Installation.InstallationId);
            var parts = repoConnection.FullName.Split('/');
            var owner = parts[0];
            var repo = parts[1];

            // Review içeriğini GitHub-friendly markdown'a çevir
            var reviewBody = FormatReviewForGitHub(msg);

            await _prService.WriteReviewCommentAsync(client, owner, repo, msg.PullNumber, reviewBody);

            _logger.LogInformation(
                "AI review written to PR #{PullNumber} | Provider={Provider} Tokens={Tokens}",
                msg.PullNumber, msg.UsedProvider, msg.PromptTokens + msg.CompletionTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to write AI review to PR #{PullNumber} for issue {IssueKey}",
                msg.PullNumber, msg.IssueKey);
            // PR'a yorum yazamama issue akışını bozmamalı
        }
    }

    /// <summary>
    /// AI review JSON'ını GitHub PR'a uygun markdown formatına çevirir
    /// </summary>
    private static string FormatReviewForGitHub(CodeReviewCompleted review)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## 🤖 ForgeFlow AI Code Review");
        sb.AppendLine();

        try
        {
            // AI bazen JSON'ı ```json ... ``` ile sarabilir, temizle
            var content = review.ReviewContent.Trim();
            if (content.StartsWith("```"))
            {
                var firstNewline = content.IndexOf('\n');
                if (firstNewline > 0)
                    content = content[(firstNewline + 1)..];
                if (content.EndsWith("```"))
                    content = content[..^3].Trim();
            }

            var json = System.Text.Json.JsonDocument.Parse(content);
            var root = json.RootElement;

            // Summary
            if (root.TryGetProperty("summary", out var summary))
            {
                sb.AppendLine($"### 📝 Summary");
                sb.AppendLine(summary.GetString());
                sb.AppendLine();
            }

            // Scores
            sb.AppendLine("### Scores");
            if (root.TryGetProperty("codeQualityScore", out var quality))
                sb.AppendLine($"- **Code Quality:** {quality.GetInt32()}/100");
            if (root.TryGetProperty("planComplianceScore", out var compliance) && compliance.ValueKind != System.Text.Json.JsonValueKind.Null)
                sb.AppendLine($"- **Plan Compliance:** {compliance.GetInt32()}/100");
            if (root.TryGetProperty("overallRating", out var rating))
                sb.AppendLine($"- **Rating:** {GetRatingEmoji(rating.GetString())} {rating.GetString()}");
            sb.AppendLine();

            // Findings
            if (root.TryGetProperty("findings", out var findings) && findings.GetArrayLength() > 0)
            {
                sb.AppendLine("### Findings");
                foreach (var finding in findings.EnumerateArray())
                {
                    var severity = finding.GetProperty("severity").GetString() ?? "info";
                    var emoji = GetSeverityEmoji(severity);
                    var message = finding.GetProperty("message").GetString();
                    var file = finding.TryGetProperty("file", out var f) ? f.GetString() : null;
                    var suggestion = finding.TryGetProperty("suggestion", out var s) ? s.GetString() : null;

                    sb.AppendLine($"- {emoji} **{severity.ToUpper()}** ");
                    if (!string.IsNullOrEmpty(file))
                        sb.AppendLine($"  `{file}`");
                    sb.AppendLine($"  {message}");
                    if (!string.IsNullOrEmpty(suggestion))
                        sb.AppendLine($"  > 💡 {suggestion}");
                    sb.AppendLine();
                }
            }
        }
        catch
        {
            // JSON parse başarısız olursa ham içeriği göster
            sb.AppendLine(review.ReviewContent);
        }

        sb.AppendLine("---");
        sb.AppendLine($"*Generated by ForgeFlow AI ({review.UsedProvider}) in {review.DurationMs}ms*");

        return sb.ToString();
    }

    private static string GetSeverityEmoji(string? severity) => severity switch
    {
        "error" => "🔴",
        "warning" => "🟡",
        "suggestion" => "🔵",
        _ => "🟢"
    };

    private static string GetRatingEmoji(string? rating) => rating switch
    {
        "APPROVE" => "✅",
        "REQUEST_CHANGES" => "❌",
        _ => "💬"
    };
}
