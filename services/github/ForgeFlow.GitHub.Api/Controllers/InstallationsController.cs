using ForgeFlow.GitHub.Domain.Entities;
using ForgeFlow.GitHub.Infrastructure.Persistence;
using ForgeFlow.GitHub.Infrastructure.GitHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Octokit;

namespace ForgeFlow.GitHub.Api.Controllers;

/// <summary>
/// GitHub App Installation yönetimi - Proje ↔ Repository bağlantısı
/// </summary>
[ApiController]
[Route("api/installations")]
public class InstallationsController : ControllerBase
{
    private readonly GitHubDbContext _db;
    private readonly ILogger<InstallationsController> _logger;
    private readonly IGitHubClientFactory _clientFactory;

    public InstallationsController(
        GitHubDbContext db, 
        ILogger<InstallationsController> logger,
        IGitHubClientFactory clientFactory)
    {
        _db = db;
        _logger = logger;
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Proje ile Repository bağlantısı kur (Manuel)
    /// </summary>
    [HttpPost("link")]
    public async Task<IActionResult> LinkProjectToRepository([FromBody] LinkProjectRequest request)
    {
        // Aynı proje için bağlantı var mı kontrol et
        var existing = await _db.RepositoryConnections
            .FirstOrDefaultAsync(r => r.ProjectId == request.ProjectId);

        if (existing != null)
        {
            return Conflict(new { error = "Project already linked to a repository", existing.FullName });
        }

        // Installation var mı kontrol et, yoksa oluştur
        var installation = await _db.Installations
            .FirstOrDefaultAsync(i => i.InstallationId == request.InstallationId);

        if (installation == null)
        {
            installation = new GitHubInstallation
            {
                Id = Guid.NewGuid(),
                InstallationId = request.InstallationId,
                AccountLogin = request.AccountLogin ?? "unknown",
                AccountType = request.AccountType ?? "Organization",
                InstalledAtUtc = DateTime.UtcNow
            };
            _db.Installations.Add(installation);
        }

        // RepositoryConnection oluştur
        var repoConnection = new RepositoryConnection
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            RepositoryId = request.RepositoryId ?? 0,
            FullName = request.RepositoryFullName,
            DefaultBranch = request.DefaultBranch ?? "main",
            InstallationId = installation.Id
        };

        _db.RepositoryConnections.Add(repoConnection);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Linked project {ProjectId} to repository {FullName} (Installation: {InstallationId})",
            request.ProjectId, request.RepositoryFullName, request.InstallationId);

        return Ok(new
        {
            message = "Repository linked successfully",
            connectionId = repoConnection.Id,
            repository = repoConnection.FullName,
            projectId = repoConnection.ProjectId
        });
    }

    /// <summary>
    /// Proje için bağlı repository bilgisini getir
    /// </summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectConnection(Guid projectId)
    {
        var connection = await _db.RepositoryConnections
            .Include(r => r.Installation)
            .FirstOrDefaultAsync(r => r.ProjectId == projectId);

        if (connection == null)
            return NotFound(new { error = "No repository connection found for this project" });

        return Ok(new
        {
            connectionId = connection.Id,
            projectId = connection.ProjectId,
            repository = connection.FullName,
            defaultBranch = connection.DefaultBranch,
            installationId = connection.Installation.InstallationId,
            accountLogin = connection.Installation.AccountLogin
        });
    }

    /// <summary>
    /// Tüm installation'ları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListInstallations()
    {
        var installations = await _db.Installations
            .Include(i => i.Repositories)
            .Select(i => new
            {
                id = i.Id,
                installationId = i.InstallationId,
                accountLogin = i.AccountLogin,
                accountType = i.AccountType,
                installedAt = i.InstalledAtUtc,
                repositoryCount = i.Repositories.Count
            })
            .ToListAsync();

        return Ok(installations);
    }

    /// <summary>
    /// Installation altındaki repoları listele
    /// </summary>
    [HttpGet("{installationId}/repositories")]
    public async Task<IActionResult> ListRepositories(long installationId)
    {
        try
        {
            var client = await _clientFactory.CreateClientAsync(installationId);
            var result = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();
            
            var repos = result.Repositories.Select(r => new
            {
                r.Id,
                r.Name,
                r.FullName,
                r.Private,
                r.HtmlUrl,
                r.DefaultBranch
            });

            return Ok(repos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list repositories for installation {InstallationId}", installationId);
            return StatusCode(500, new { error = "Failed to fetch repositories from GitHub" });
        }
    }

    /// <summary>
    /// Bağlantıyı sil
    /// </summary>
    [HttpDelete("project/{projectId}")]
    public async Task<IActionResult> UnlinkProject(Guid projectId)
    {
        var connection = await _db.RepositoryConnections
            .FirstOrDefaultAsync(r => r.ProjectId == projectId);

        if (connection == null)
            return NotFound();

        _db.RepositoryConnections.Remove(connection);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Unlinked project {ProjectId} from repository", projectId);
        return NoContent();
    }
}

public record LinkProjectRequest(
    Guid ProjectId,
    long InstallationId,
    string RepositoryFullName,
    string? DefaultBranch = "main",
    long? RepositoryId = null,
    string? AccountLogin = null,
    string? AccountType = null
);
