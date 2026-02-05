using ForgeFlow.GitHub.Infrastructure.GitHub;
using ForgeFlow.GitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// Repository içerik servisi implementasyonu - AI Orchestrator entegrasyonu
/// </summary>
public class RepositoryContentService : IRepositoryContentService
{
    private readonly IGitHubClientFactory _clientFactory;
    private readonly GitHubDbContext _db;
    private readonly ILogger<RepositoryContentService> _logger;

    public RepositoryContentService(
        IGitHubClientFactory clientFactory,
        GitHubDbContext db,
        ILogger<RepositoryContentService> logger)
    {
        _clientFactory = clientFactory;
        _db = db;
        _logger = logger;
    }

    public async Task<RepositoryTreeDto> GetTreeAsync(Guid projectId, string? path)
    {
        var repo = await GetRepoConnectionAsync(projectId);
        var client = await _clientFactory.CreateClientAsync(repo.Installation.InstallationId);

        var (owner, repoName) = ParseFullName(repo.FullName);

        try
        {
            var contents = string.IsNullOrEmpty(path)
                ? await client.Repository.Content.GetAllContents(owner, repoName)
                : await client.Repository.Content.GetAllContents(owner, repoName, path);

            var items = contents.Select(c => new TreeItemDto(
                c.Path,
                c.Name,
                c.Type.Value == ContentType.File ? "file" : "dir",
                c.Size
            )).ToArray();

            _logger.LogInformation(
                "Retrieved tree for {Owner}/{Repo} at path '{Path}': {Count} items",
                owner, repoName, path ?? "/", items.Length);

            return new RepositoryTreeDto(path ?? "", items);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Path not found: {Owner}/{Repo}/{Path}", owner, repoName, path);
            return new RepositoryTreeDto(path ?? "", []);
        }
    }

    public async Task<Dictionary<string, string>> GetFilesAsync(Guid projectId, string[] filePaths)
    {
        var repo = await GetRepoConnectionAsync(projectId);
        var client = await _clientFactory.CreateClientAsync(repo.Installation.InstallationId);

        var (owner, repoName) = ParseFullName(repo.FullName);
        var result = new Dictionary<string, string>();

        foreach (var filePath in filePaths)
        {
            try
            {
                var contents = await client.Repository.Content.GetAllContents(owner, repoName, filePath);
                var file = contents.FirstOrDefault();

                if (file?.Type.Value == ContentType.File && file.Content != null)
                {
                    result[filePath] = file.Content;
                    _logger.LogDebug("Retrieved content for {FilePath} ({Size} bytes)", filePath, file.Size);
                }
            }
            catch (NotFoundException)
            {
                _logger.LogWarning("File not found: {Owner}/{Repo}/{FilePath}", owner, repoName, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file: {Owner}/{Repo}/{FilePath}", owner, repoName, filePath);
            }
        }

        _logger.LogInformation(
            "Retrieved {Count}/{Total} files for {Owner}/{Repo}",
            result.Count, filePaths.Length, owner, repoName);

        return result;
    }

    private async Task<Domain.Entities.RepositoryConnection> GetRepoConnectionAsync(Guid projectId)
    {
        var repo = await _db.RepositoryConnections
            .Include(r => r.Installation)
            .FirstOrDefaultAsync(r => r.ProjectId == projectId);

        if (repo == null)
            throw new InvalidOperationException($"No repository connection found for project {projectId}");

        return repo;
    }

    private static (string owner, string repo) ParseFullName(string fullName)
    {
        var parts = fullName.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid repository full name: {fullName}");

        return (parts[0], parts[1]);
    }
}
