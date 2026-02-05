using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

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
                case "installation":
                    await HandleInstallationEvent(payload);
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

        _logger.LogInformation("PR event: Action={Action}, PR=#{PrNumber}", action, prNumber);

        // PR merged olduğunda
        if (action == "closed" && pr.GetProperty("merged").GetBoolean())
        {
            var branchName = pr.GetProperty("head").GetProperty("ref").GetString() ?? "";
            var issueKey = ExtractIssueKey(branchName);

            if (!string.IsNullOrEmpty(issueKey))
            {
                await _publishEndpoint.Publish(new GitHubPullRequestMerged(
                    repoId,
                    prNumber,
                    issueKey
                ));

                _logger.LogInformation(
                    "Published GitHubPullRequestMerged: PR=#{PrNumber}, Issue={IssueKey}",
                    prNumber, issueKey);
            }
        }
    }

    private Task HandleInstallationEvent(JsonDocument payload)
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

        // TODO: Installation'ı veritabanına kaydet
        // Bu kısım ileride implemente edilecek

        return Task.CompletedTask;
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
}
