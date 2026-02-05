using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// Branch oluşturma servisi implementasyonu
/// </summary>
public class BranchService : IBranchService
{
    private readonly ILogger<BranchService> _logger;

    public BranchService(ILogger<BranchService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateFeatureBranchAsync(
        IGitHubClient client,
        string owner,
        string repo,
        string issueKey,
        string issueTitle,
        string? defaultBranch)
    {
        // 1. Base branch belirleme stratejisi
        // Öncelik: develop -> DB'den gelen defaultBranch -> master -> main -> Repo'nun gerçek default branch'i
        
        // Önce develop var mı kontrol et
        try
        {
            await client.Repository.Branch.Get(owner, repo, "develop");
            
            // Develop bulundu, base olarak kullan
            var developRef = await client.Git.Reference.Get(owner, repo, "heads/develop");
            return await CreateBranchInternalAsync(client, owner, repo, issueKey, issueTitle, "develop", developRef.Object.Sha);
        }
        catch (NotFoundException)
        {
            _logger.LogInformation("No 'develop' branch found for {Owner}/{Repo}", owner, repo);
        }

        // Develop yok, diğer seçenekleri dene
        string[] candidates = new[] { defaultBranch, "master", "main" }
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .ToArray();

        foreach (var branch in candidates)
        {
            try
            {
                var refObj = await client.Git.Reference.Get(owner, repo, $"heads/{branch}");
                _logger.LogInformation("Found base branch '{Branch}' for {Owner}/{Repo}", branch, owner, repo);
                return await CreateBranchInternalAsync(client, owner, repo, issueKey, issueTitle, branch!, refObj.Object.Sha);
            }
            catch (NotFoundException)
            {
                // Bu branch yok, sonrakini dene
            }
        }

        // Hiçbiri bulunamadı, son çare olarak Repo bilgisini çekip DefaultBranch'i öğren
        try 
        {
            var repository = await client.Repository.Get(owner, repo);
            var realDefault = repository.DefaultBranch;
             
            var refObj = await client.Git.Reference.Get(owner, repo, $"heads/{realDefault}");
            _logger.LogInformation("Using repository default branch '{Branch}' for {Owner}/{Repo}", realDefault, owner, repo);
            return await CreateBranchInternalAsync(client, owner, repo, issueKey, issueTitle, realDefault, refObj.Object.Sha);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to determine any valid base branch for {Owner}/{Repo}", owner, repo);
             throw new InvalidOperationException($"Could not find a valid base branch (tried develop, {string.Join(",", candidates)}) and failed to get repo default.");
        }
    }

    private async Task<string> CreateBranchInternalAsync(IGitHubClient client, string owner, string repo, string issueKey, string issueTitle, string baseBranch, string baseSha)
    {
        // 3. Branch adını URL-friendly slug formatına çevir
        var slug = CreateSlug(issueTitle);
        var branchName = $"feature/{issueKey}-{slug}";

        // 4. Yeni branch oluştur
        try
        {
            await client.Git.Reference.Create(owner, repo, new NewReference($"refs/heads/{branchName}", baseSha));
            _logger.LogInformation(
                "Created branch '{BranchName}' from '{BaseBranch}' at SHA {Sha} for {Owner}/{Repo}",
                branchName, baseBranch, baseSha[..7], owner, repo);
        }
        catch (ApiException ex) when (ex.Message.Contains("Reference already exists"))
        {
            _logger.LogWarning("Branch '{BranchName}' already exists for {Owner}/{Repo}", branchName, owner, repo);
        }

        return branchName;
    }

    /// <summary>
    /// Issue title'ı URL-friendly slug'a çevirir
    /// </summary>
    private static string CreateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "issue";

        // Küçük harfe çevir, alfanumerik olmayan karakterleri tire ile değiştir
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
        slug = slug.Trim('-');

        // Maksimum 50 karakter
        if (slug.Length > 50)
            slug = slug[..50].TrimEnd('-');

        return string.IsNullOrEmpty(slug) ? "issue" : slug;
    }
}
