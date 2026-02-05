using ForgeFlow.Contracts.Events;
using ForgeFlow.GitHub.Application.Services;
using ForgeFlow.GitHub.Infrastructure.GitHub;
using ForgeFlow.GitHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.GitHub.Application.Consumers;

/// <summary>
/// IssueAssigned event consumer - Otomatik feature branch oluşturma
/// </summary>
public class IssueAssignedConsumer : IConsumer<IssueAssigned>
{
    private readonly IGitHubClientFactory _clientFactory;
    private readonly IBranchService _branchService;
    private readonly GitHubDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IssueAssignedConsumer> _logger;

    public IssueAssignedConsumer(
        IGitHubClientFactory clientFactory,
        IBranchService branchService,
        GitHubDbContext db,
        IPublishEndpoint publishEndpoint,
        ILogger<IssueAssignedConsumer> logger)
    {
        _clientFactory = clientFactory;
        _branchService = branchService;
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _logger.LogInformation("IssueAssignedConsumer initialized");
    }

    public async Task Consume(ConsumeContext<IssueAssigned> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Received IssueAssigned event: {IssueKey} assigned to {NewAssigneeId}",
            msg.IssueKey, msg.NewAssigneeId);

        // Yeni atama yoksa (unassign) veya repo URL yoksa çık
        if (string.IsNullOrEmpty(msg.NewAssigneeId))
        {
            _logger.LogDebug("Skipping - no new assignee (unassign event)");
            return;
        }

        if (string.IsNullOrEmpty(msg.RepositoryUrl))
        {
            _logger.LogDebug("Skipping - no repository URL configured for project");
            return;
        }

        // Repository bağlantısını bul
        var repoConnection = await _db.RepositoryConnections
            .Include(r => r.Installation)
            .FirstOrDefaultAsync(r => r.ProjectId == msg.ProjectId);

        // ON-DEMAND FALLBACK: Bağlantı yoksa RepositoryUrl'den bulmaya çalış
        if (repoConnection == null && !string.IsNullOrEmpty(msg.RepositoryUrl))
        {
            var fullName = ExtractFullNameFromUrl(msg.RepositoryUrl);
            if (fullName != null)
            {
                // FullName ile eşleşen installation bul
                repoConnection = await _db.RepositoryConnections
                    .Include(r => r.Installation)
                    .FirstOrDefaultAsync(r => r.FullName.ToLower() == fullName.ToLower());

                // Hala yoksa, mevcut installation'lardan biri ile oluştur
                if (repoConnection == null)
                {
                    var installation = await _db.Installations.FirstOrDefaultAsync();
                    if (installation != null)
                    {
                        repoConnection = new Domain.Entities.RepositoryConnection
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = msg.ProjectId,
                            FullName = fullName,
                            DefaultBranch = msg.DefaultBranch ?? "main",
                            InstallationId = installation.Id
                        };
                        _db.RepositoryConnections.Add(repoConnection);
                        await _db.SaveChangesAsync();

                        _logger.LogInformation(
                            "Created on-demand RepositoryConnection for project {ProjectId} → {FullName}",
                            msg.ProjectId, fullName);
                    }
                }
            }
        }

        if (repoConnection == null)
        {
            _logger.LogWarning(
                "No repository connection found for project {ProjectId}, skipping branch creation. " +
                "Use POST /api/installations/link to create a connection.",
                msg.ProjectId);
            return;
        }

        try
        {
            // GitHub client oluştur
            var client = await _clientFactory.CreateClientAsync(repoConnection.Installation.InstallationId);

            // Branch oluştur
            var (owner, repo) = ParseFullName(repoConnection.FullName);
            var branchName = await _branchService.CreateFeatureBranchAsync(
                client, owner, repo,
                msg.IssueKey, msg.IssueTitle,
                repoConnection.DefaultBranch);

            _logger.LogInformation(
                "Created feature branch '{BranchName}' for issue {IssueKey}",
                branchName, msg.IssueKey);

            // BranchCreated event publish
            await _publishEndpoint.Publish(new BranchCreated(
                msg.ProjectId,
                msg.IssueKey,
                branchName,
                msg.NewAssigneeId,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create branch for issue {IssueKey} in {FullName}",
                msg.IssueKey, repoConnection.FullName);
            throw; // Retry mekanizması için
        }
    }

    private static (string owner, string repo) ParseFullName(string fullName)
    {
        var parts = fullName.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid repository full name: {fullName}");

        return (parts[0], parts[1]);
    }

    /// <summary>
    /// GitHub URL'inden owner/repo formatını çıkarır
    /// Örnek: https://github.com/owner/repo.git → owner/repo
    /// </summary>
    private static string? ExtractFullNameFromUrl(string url)
    {
        try
        {
            // https://github.com/owner/repo veya https://github.com/owner/repo.git
            var uri = new Uri(url);
            var path = uri.AbsolutePath.Trim('/');
            
            // .git uzantısını kaldır
            if (path.EndsWith(".git"))
                path = path[..^4];
            
            var parts = path.Split('/');
            if (parts.Length >= 2)
                return $"{parts[0]}/{parts[1]}";
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}
