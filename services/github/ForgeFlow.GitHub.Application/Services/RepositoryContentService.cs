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

    /// <summary>
    /// Binary dosya uzantıları — recursive tree'den filtrelenir
    /// </summary>
    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg", ".webp",
        ".mp4", ".mp3", ".wav", ".avi", ".mov",
        ".zip", ".tar", ".gz", ".rar", ".7z",
        ".dll", ".exe", ".bin", ".obj", ".o", ".so", ".dylib",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
        ".lock", ".min.js", ".min.css"
    };

    /// <summary>
    /// Kritik dosya pattern'ları — repo analizi sırasında otomatik tespit edilir
    /// </summary>
    private static readonly string[] CriticalFilePatterns =
    [
        "*.sln",
        "*.csproj",
        "package.json",
        "tsconfig.json",
        "Dockerfile",
        "docker-compose.yml",
        "docker-compose.yaml",
        ".github/workflows/*.yml",
        ".github/workflows/*.yaml",
        "vite.config.ts",
        "vite.config.js",
        "webpack.config.js",
        "webpack.config.ts",
        "next.config.js",
        "next.config.mjs",
        ".eslintrc.json",
        "eslint.config.js"
    ];

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

    public async Task<RepositoryTreeDto> GetRecursiveTreeAsync(Guid projectId, int maxItems = 500)
    {
        var repo = await GetRepoConnectionAsync(projectId);
        var client = await _clientFactory.CreateClientAsync(repo.Installation.InstallationId);

        var (owner, repoName) = ParseFullName(repo.FullName);
        
        // Always try to get the real default branch from GitHub if possible to avoid main/master mismatch
        string defaultBranch;
        try
        {
            var ghRepo = await client.Repository.Get(owner, repoName);
            defaultBranch = ghRepo.DefaultBranch;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch repository details for {Owner}/{Repo}, fallback to '{Fallback}'", owner, repoName, repo.DefaultBranch ?? "main");
            defaultBranch = repo.DefaultBranch ?? "main";
        }

        try
        {
            // Git Trees API ile recursive tree çek
            var treeResponse = await client.Git.Tree.GetRecursive(owner, repoName, defaultBranch);

            var items = treeResponse.Tree
                .Where(t => !IsBinaryFile(t.Path))          // Binary dosyaları filtrele
                .Where(t => !IsIgnoredPath(t.Path))          // node_modules, .git vb. filtrele
                .Take(maxItems)                               // Max item limiti
                .Select(t => new TreeItemDto(
                    t.Path,
                    System.IO.Path.GetFileName(t.Path),
                    t.Type.Value == TreeType.Blob ? "file" : "dir",
                    t.Size
                ))
                .ToArray();

            _logger.LogInformation(
                "Retrieved recursive tree for {Owner}/{Repo}: {Count} items (truncated={Truncated}, filtered from {Total})",
                owner, repoName, items.Length, treeResponse.Truncated, treeResponse.Tree.Count);

            return new RepositoryTreeDto("", items);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Branch '{Branch}' not found for {Owner}/{Repo}", defaultBranch, owner, repoName);
            return new RepositoryTreeDto("", []);
        }
    }

    public async Task<RepoAnalysisResult> AnalyzeRepoAsync(Guid projectId)
    {
        _logger.LogInformation("Starting repo analysis for project {ProjectId}", projectId);

        // 1. Recursive tree çek
        var tree = await GetRecursiveTreeAsync(projectId);
        var treePaths = tree.Items.Select(i => i.Path).ToArray();

        // 2. Kritik dosyaları tespit et
        var criticalFilePaths = treePaths
            .Where(IsCriticalFile)
            .Take(20) // Max 20 kritik dosya
            .ToArray();

        _logger.LogInformation("Found {Count} critical files in project {ProjectId}", criticalFilePaths.Length, projectId);

        // 3. Kritik dosya içeriklerini çek
        var criticalFiles = criticalFilePaths.Length > 0
            ? await GetFilesAsync(projectId, criticalFilePaths)
            : new Dictionary<string, string>();

        // 4. Tech stack tespit et
        var detectedTechStack = DetectTechStack(treePaths, criticalFiles);

        // 5. Mevcut workflow'ları tespit et
        var existingWorkflows = treePaths
            .Where(p => p.StartsWith(".github/workflows/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        _logger.LogInformation(
            "Repo analysis complete for {ProjectId}: {TreeCount} files, {CriticalCount} critical, {TechCount} tech stack items, {WorkflowCount} existing workflows",
            projectId, treePaths.Length, criticalFiles.Count, detectedTechStack.Length, existingWorkflows.Length);

        return new RepoAnalysisResult(
            TreePaths: treePaths,
            CriticalFiles: criticalFiles,
            DetectedTechStack: detectedTechStack,
            ExistingWorkflows: existingWorkflows
        );
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

    #region Private Helpers

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

    private static bool IsBinaryFile(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && BinaryExtensions.Contains(ext);
    }

    private static bool IsIgnoredPath(string path)
    {
        var segments = path.Split('/');
        return segments.Any(s =>
            s == "node_modules" ||
            s == ".git" ||
            s == "bin" ||
            s == "obj" ||
            s == "dist" ||
            s == ".vs" ||
            s == ".idea" ||
            s == "__pycache__" ||
            s == "packages" ||
            s.StartsWith('.') && s != ".github" && s != ".eslintrc.json");
    }

    /// <summary>
    /// Dosya yolunun kritik dosya kalıplarına uyup uymadığını kontrol eder
    /// </summary>
    private static bool IsCriticalFile(string path)
    {
        var fileName = System.IO.Path.GetFileName(path);
        var ext = System.IO.Path.GetExtension(path);

        // Doğrudan isim eşleşmesi
        if (fileName is "package.json" or "tsconfig.json" or "Dockerfile"
            or "docker-compose.yml" or "docker-compose.yaml"
            or "vite.config.ts" or "vite.config.js"
            or "webpack.config.js" or "webpack.config.ts"
            or "next.config.js" or "next.config.mjs"
            or ".eslintrc.json" or "eslint.config.js")
        {
            return true;
        }

        // Uzantı eşleşmesi
        if (ext is ".sln" or ".csproj")
            return true;

        // Workflow dosyaları
        if (path.StartsWith(".github/workflows/", StringComparison.OrdinalIgnoreCase)
            && ext is ".yml" or ".yaml")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Dosya ağacı ve kritik dosya içeriklerinden tech stack tespit eder
    /// </summary>
    private static string[] DetectTechStack(string[] treePaths, Dictionary<string, string> criticalFiles)
    {
        var techStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Dosya uzantılarından tespit
        foreach (var path in treePaths)
        {
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".cs": techStack.Add("C#"); break;
                case ".ts" or ".tsx": techStack.Add("TypeScript"); break;
                case ".js" or ".jsx": techStack.Add("JavaScript"); break;
                case ".py": techStack.Add("Python"); break;
                case ".java": techStack.Add("Java"); break;
                case ".go": techStack.Add("Go"); break;
                case ".rs": techStack.Add("Rust"); break;
                case ".rb": techStack.Add("Ruby"); break;
                case ".php": techStack.Add("PHP"); break;
            }
        }

        // Kritik dosya varlığından tespit
        foreach (var path in treePaths)
        {
            var fileName = System.IO.Path.GetFileName(path);
            switch (fileName)
            {
                case "Dockerfile" or "docker-compose.yml" or "docker-compose.yaml":
                    techStack.Add("Docker"); break;
            }

            if (System.IO.Path.GetExtension(path) == ".sln")
                techStack.Add(".NET");

            if (fileName == "vite.config.ts" || fileName == "vite.config.js")
                techStack.Add("Vite");

            if (fileName == "next.config.js" || fileName == "next.config.mjs")
                techStack.Add("Next.js");

            if (fileName == "webpack.config.js" || fileName == "webpack.config.ts")
                techStack.Add("Webpack");
        }

        // package.json içeriğinden tespit
        foreach (var (filePath, content) in criticalFiles)
        {
            if (System.IO.Path.GetFileName(filePath) == "package.json")
            {
                if (content.Contains("\"react\"")) techStack.Add("React");
                if (content.Contains("\"vue\"")) techStack.Add("Vue");
                if (content.Contains("\"angular\"") || content.Contains("\"@angular/core\"")) techStack.Add("Angular");
                if (content.Contains("\"express\"")) techStack.Add("Express.js");
                if (content.Contains("\"next\"")) techStack.Add("Next.js");
                if (content.Contains("\"vite\"")) techStack.Add("Vite");
            }

            // .csproj'dan framework tespiti
            if (System.IO.Path.GetExtension(filePath) == ".csproj")
            {
                if (content.Contains("net8.0")) techStack.Add(".NET 8");
                else if (content.Contains("net9.0")) techStack.Add(".NET 9");
                else if (content.Contains("net7.0")) techStack.Add(".NET 7");

                if (content.Contains("xunit") || content.Contains("xUnit")) techStack.Add("xUnit");
                if (content.Contains("NUnit")) techStack.Add("NUnit");
                if (content.Contains("MSTest")) techStack.Add("MSTest");
            }
        }

        return techStack.ToArray();
    }

    #endregion
}
