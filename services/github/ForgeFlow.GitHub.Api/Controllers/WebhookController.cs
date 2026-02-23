using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.GitHub.Api.Controllers;

/// <summary>
/// GitHub Webhook handler - Push, PR, Installation event'leri
/// </summary>
[ApiController]
[Route("api/github")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IConfiguration config,
        IPublishEndpoint publishEndpoint,
        ILogger<WebhookController> logger)
    {
        _config = config;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        // 1. Body'yi oku
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        // 2. HMAC SHA256 Signature doğrula
        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (!ValidateSignature(body, signature))
        {
            _logger.LogWarning("Invalid webhook signature received");
            return Unauthorized("Invalid signature");
        }

        // 3. Event tipine göre işle
        var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

        _logger.LogInformation(
            "Received GitHub webhook: Event={EventType}, DeliveryId={DeliveryId}",
            eventType, deliveryId);

        try
        {
            using var payload = JsonDocument.Parse(body);

            switch (eventType)
            {
                case "push":
                    await HandlePushEvent(payload);
                    break;
                case "pull_request":
                    await HandlePullRequestEvent(payload);
                    break;
                case "check_suite":
                    await HandleCheckSuiteEvent(payload);
                    break;
                case "workflow_run":
                    await HandleWorkflowRunEvent(payload);
                    break;
                case "installation":
                    await HandleInstallationEvent(payload);
                    break;
                case "installation_repositories":
                    await HandleInstallationRepositoriesEvent(payload);
                    break;
                default:
                    _logger.LogDebug("Unhandled event type: {EventType}", eventType);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook: {EventType}", eventType);
            return StatusCode(500, "Error processing webhook");
        }
    }

    private bool ValidateSignature(string payload, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        var secret = _config["GitHub:WebhookSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("GitHub:WebhookSecret not configured, skipping validation");
            return true; // Development modunda geç
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expected));
    }

    private async Task HandlePushEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var repoId = root.GetProperty("repository").GetProperty("id").GetInt64();
        var refName = root.GetProperty("ref").GetString() ?? "";
        var headCommit = root.GetProperty("head_commit");
        var headSha = headCommit.GetProperty("id").GetString() ?? "";

        // Modified files
        var modified = new List<string>();
        if (root.TryGetProperty("commits", out var commits))
        {
            foreach (var commit in commits.EnumerateArray())
            {
                if (commit.TryGetProperty("modified", out var mods))
                    modified.AddRange(mods.EnumerateArray().Select(m => m.GetString()!));
                if (commit.TryGetProperty("added", out var added))
                    modified.AddRange(added.EnumerateArray().Select(m => m.GetString()!));
            }
        }

        await _publishEndpoint.Publish(new GitHubPushReceived(
            repoId,
            refName,
            headSha,
            modified.Distinct().ToArray(),
            DateTime.UtcNow
        ));

        _logger.LogInformation(
            "Published GitHubPushReceived: Repo={RepoId}, Ref={Ref}, Files={FileCount}",
            repoId, refName, modified.Count);
    }

    private async Task HandlePullRequestEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var action = root.GetProperty("action").GetString();
        var pr = root.GetProperty("pull_request");
        var repoId = root.GetProperty("repository").GetProperty("id").GetInt64();
        var prNumber = pr.GetProperty("number").GetInt32();
        var branchName = pr.GetProperty("head").GetProperty("ref").GetString() ?? "";
        var issueKey = ExtractIssueKey(branchName);

        _logger.LogInformation("PR event: Action={Action}, PR=#{PrNumber}, Branch={Branch}, IssueKey={IssueKey}",
            action, prNumber, branchName, issueKey ?? "N/A");

        if (string.IsNullOrEmpty(issueKey))
        {
            _logger.LogDebug("No issue key found in branch name: {Branch}", branchName);
            return;
        }

        switch (action)
        {
            // PR açıldığında → Issue otomatik "In Review"
            case "opened":
            case "reopened":
                {
                    var prTitle = pr.GetProperty("title").GetString() ?? "";
                    var prUrl = pr.GetProperty("html_url").GetString() ?? "";
                    var authorLogin = pr.GetProperty("user").GetProperty("login").GetString() ?? "";

                    // Repo bilgileri
                    var repoFullName = root.GetProperty("repository").GetProperty("full_name").GetString() ?? "";
                    var repoParts = repoFullName.Split('/');
                    var repoOwner = repoParts.Length >= 2 ? repoParts[0] : "";
                    var repoName = repoParts.Length >= 2 ? repoParts[1] : "";

                    // Installation ID (GitHub App webhook payload'ında bulunur)
                    long installationId = 0;
                    if (root.TryGetProperty("installation", out var installation))
                    {
                        installationId = installation.GetProperty("id").GetInt64();
                    }

                    // Issue durumu güncelleme event'i
                    await _publishEndpoint.Publish(new GitHubPullRequestOpened(
                        repoId, prNumber, issueKey, prTitle, prUrl, authorLogin, DateTime.UtcNow
                    ));

                    // AI Code Review tetikleme event'i
                    if (installationId > 0)
                    {
                        await _publishEndpoint.Publish(new CodeReviewRequested(
                            IssueKey: issueKey,
                            ProjectId: "", // Consumer, RepositoryConnection tablosundan çözecek
                            PullNumber: prNumber,
                            PrTitle: prTitle,
                            PrUrl: prUrl,
                            RepositoryOwner: repoOwner,
                            RepositoryName: repoName,
                            InstallationId: installationId,
                            Timestamp: DateTime.UtcNow
                        ));

                        _logger.LogInformation(
                            "Published CodeReviewRequested: PR=#{PrNumber}, Issue={IssueKey}, Repo={Owner}/{Repo}",
                            prNumber, issueKey, repoOwner, repoName);
                    }

                    // PR durumu takibi için Artifact Service'e bildir
                    await _publishEndpoint.Publish(new PullRequestStatusChanged(
                        issueKey, prNumber, "open", DateTime.UtcNow));

                    _logger.LogInformation(
                        "Published GitHubPullRequestOpened: PR=#{PrNumber}, Issue={IssueKey}, Author={Author}",
                        prNumber, issueKey, authorLogin);
                    break;
                }

            // PR merge edildiğinde → Issue otomatik "Done"
            case "closed" when pr.GetProperty("merged").GetBoolean():
                {
                    await _publishEndpoint.Publish(new GitHubPullRequestMerged(
                        repoId, prNumber, issueKey
                    ));

                    // PR durumu takibi
                    await _publishEndpoint.Publish(new PullRequestStatusChanged(
                        issueKey, prNumber, "merged", DateTime.UtcNow));

                    _logger.LogInformation(
                        "Published GitHubPullRequestMerged: PR=#{PrNumber}, Issue={IssueKey}",
                        prNumber, issueKey);
                    break;
                }

            // PR merge olmadan kapatıldığında → Issue "In Progress"e geri
            case "closed":
                {
                    await _publishEndpoint.Publish(new GitHubPullRequestClosed(
                        repoId, prNumber, issueKey, DateTime.UtcNow
                    ));

                    // PR durumu takibi
                    await _publishEndpoint.Publish(new PullRequestStatusChanged(
                        issueKey, prNumber, "closed", DateTime.UtcNow));

                    _logger.LogInformation(
                        "Published GitHubPullRequestClosed (not merged): PR=#{PrNumber}, Issue={IssueKey}",
                        prNumber, issueKey);
                    break;
                }
        }
    }

    /// <summary>
    /// GitHub Actions check_suite tamamlandığında CI/CD durumunu yayınlar.
    /// Sadece "completed" action'ını işler (noise azaltma).
    /// </summary>
    private async Task HandleCheckSuiteEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var action = root.GetProperty("action").GetString();

        // Sadece tamamlanan check suite'leri işle
        if (action != "completed")
        {
            _logger.LogDebug("Skipping check_suite action: {Action}", action);
            return;
        }

        var checkSuite = root.GetProperty("check_suite");
        var branchName = checkSuite.GetProperty("head_branch").GetString() ?? "";
        var issueKey = ExtractIssueKey(branchName);

        if (string.IsNullOrEmpty(issueKey))
        {
            _logger.LogDebug("No issue key found in check_suite branch: {Branch}", branchName);
            return;
        }

        var repoId = root.GetProperty("repository").GetProperty("id").GetInt64();
        var conclusion = checkSuite.GetProperty("conclusion").GetString() ?? "unknown";
        var headSha = checkSuite.GetProperty("head_sha").GetString() ?? "";

        // check_suite'in kendi ID'sini RunId olarak kullan
        var suiteId = checkSuite.GetProperty("id").GetInt64();

        // HTML URL: check_suite doğrudan URL vermez, repo URL'i + commit SHA ile oluştur
        var repoUrl = root.GetProperty("repository").GetProperty("html_url").GetString() ?? "";
        var htmlUrl = !string.IsNullOrEmpty(repoUrl) ? $"{repoUrl}/commit/{headSha}/checks" : null;

        await _publishEndpoint.Publish(new CiCdStatusReceived(
            IssueKey: issueKey,
            RepositoryId: repoId,
            WorkflowName: "Check Suite",
            Status: "completed",
            Conclusion: conclusion,
            BranchName: branchName,
            CommitSha: headSha,
            HtmlUrl: htmlUrl,
            RunId: suiteId,
            Timestamp: DateTime.UtcNow
        ));

        _logger.LogInformation(
            "Published CiCdStatusReceived (check_suite): Issue={IssueKey}, Conclusion={Conclusion}, SHA={Sha}",
            issueKey, conclusion, headSha[..Math.Min(7, headSha.Length)]);
    }

    /// <summary>
    /// GitHub Actions workflow_run lifecycle event'lerini işler.
    /// Tüm durumları takip eder: requested(queued) → in_progress → completed.
    /// </summary>
    private async Task HandleWorkflowRunEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var action = root.GetProperty("action").GetString();
        var workflowRun = root.GetProperty("workflow_run");
        var branchName = workflowRun.GetProperty("head_branch").GetString() ?? "";
        var issueKey = ExtractIssueKey(branchName);

        if (string.IsNullOrEmpty(issueKey))
        {
            _logger.LogDebug("No issue key found in workflow_run branch: {Branch}", branchName);
            return;
        }

        var repoId = root.GetProperty("repository").GetProperty("id").GetInt64();
        var workflowName = workflowRun.GetProperty("name").GetString() ?? "Unknown Workflow";
        var headSha = workflowRun.GetProperty("head_sha").GetString() ?? "";
        var htmlUrl = workflowRun.GetProperty("html_url").GetString();
        var runId = workflowRun.GetProperty("id").GetInt64();

        // Action → Status mapping
        var status = action switch
        {
            "requested" => "queued",
            "in_progress" => "in_progress",
            "completed" => "completed",
            _ => action ?? "unknown"
        };

        // Conclusion sadece completed durumunda anlamlı
        string? conclusion = null;
        if (status == "completed" && workflowRun.TryGetProperty("conclusion", out var conclusionProp))
        {
            conclusion = conclusionProp.GetString();
        }

        await _publishEndpoint.Publish(new CiCdStatusReceived(
            IssueKey: issueKey,
            RepositoryId: repoId,
            WorkflowName: workflowName,
            Status: status,
            Conclusion: conclusion,
            BranchName: branchName,
            CommitSha: headSha,
            HtmlUrl: htmlUrl,
            RunId: runId,
            Timestamp: DateTime.UtcNow
        ));

        _logger.LogInformation(
            "Published CiCdStatusReceived (workflow_run): Issue={IssueKey}, Workflow={Workflow}, Status={Status}, Conclusion={Conclusion}",
            issueKey, workflowName, status, conclusion ?? "N/A");
    }

    private async Task HandleInstallationEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var action = root.GetProperty("action").GetString();
        var installation = root.GetProperty("installation");
        var installationId = installation.GetProperty("id").GetInt64();
        var account = installation.GetProperty("account");
        var login = account.GetProperty("login").GetString() ?? "";
        var accountType = account.GetProperty("type").GetString() ?? "";

        _logger.LogInformation(
            "Installation event: Action={Action}, InstallationId={Id}, Account={Login}",
            action, installationId, login);

        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeFlow.GitHub.Infrastructure.Persistence.GitHubDbContext>();

        var existing = await db.Installations
            .FirstOrDefaultAsync(i => i.InstallationId == installationId);

        if (action == "created" || action == "unsuspend")
        {
            if (existing == null)
            {
                var newInstallation = new ForgeFlow.GitHub.Domain.Entities.GitHubInstallation
                {
                    Id = Guid.NewGuid(),
                    InstallationId = installationId,
                    AccountLogin = login,
                    AccountType = accountType,
                    InstalledAtUtc = DateTime.UtcNow
                };
                db.Installations.Add(newInstallation);
                _logger.LogInformation("New installation registered: {InstallationId}", installationId);

                await _publishEndpoint.Publish(new GitHubInstallationCreated(
                    installationId,
                    login,
                    accountType,
                    DateTime.UtcNow
                ));
            }
            else
            {
                existing.AccountLogin = login; // Update login if changed
                existing.AccountType = accountType;
                _logger.LogInformation("Installation updated: {InstallationId}", installationId);
            }
            await db.SaveChangesAsync();
        }
        else if (action == "deleted")
        {
            if (existing != null)
            {
                db.Installations.Remove(existing);
                await db.SaveChangesAsync();
                _logger.LogInformation("Installation deleted: {InstallationId}", installationId);
            }
        }
    }

    /// <summary>
    /// Branch adından issue key'i çıkarır
    /// Örnek: "feature/FORGE-123-add-login" → "FORGE-123"
    /// </summary>
    private static string? ExtractIssueKey(string branchName)
    {
        // feature/PROJ-123-xxx veya PROJ-123-xxx formatı
        var match = System.Text.RegularExpressions.Regex.Match(
            branchName,
            @"([A-Z]+-\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private async Task HandleInstallationRepositoriesEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var action = root.GetProperty("action").GetString();
        var installation = root.GetProperty("installation");
        var installationId = installation.GetProperty("id").GetInt64();
        var account = installation.GetProperty("account");
        var login = account.GetProperty("login").GetString() ?? "";
        var accountType = account.GetProperty("type").GetString() ?? "";

        _logger.LogInformation(
            "Installation Repositories event: Action={Action}, InstallationId={Id}, Account={Login}",
            action, installationId, login);

        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeFlow.GitHub.Infrastructure.Persistence.GitHubDbContext>();

        var existing = await db.Installations
            .FirstOrDefaultAsync(i => i.InstallationId == installationId);

        // repo added/removed fark etmeksizin installation kaydını garanti altına al (upsert)
        if (existing == null)
        {
            var newInstallation = new ForgeFlow.GitHub.Domain.Entities.GitHubInstallation
            {
                Id = Guid.NewGuid(),
                InstallationId = installationId,
                AccountLogin = login,
                AccountType = accountType,
                InstalledAtUtc = DateTime.UtcNow
            };
            db.Installations.Add(newInstallation);
            _logger.LogInformation("New installation registered (via repo event): {InstallationId}", installationId);

            await _publishEndpoint.Publish(new GitHubInstallationCreated(
                installationId,
                login,
                accountType,
                DateTime.UtcNow
            ));
        }
        else
        {
            // Update info if changed
            existing.AccountLogin = login;
            existing.AccountType = accountType;
        }

        await db.SaveChangesAsync();
    }
}
