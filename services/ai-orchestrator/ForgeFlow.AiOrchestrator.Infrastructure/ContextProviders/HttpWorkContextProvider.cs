using System.Net.Http.Json;
using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForgeFlow.AiOrchestrator.Infrastructure.ContextProviders;

/// <summary>
/// Configuration options for Work Service and GitHub Service clients
/// </summary>
public class WorkServiceOptions
{
    public const string SectionName = "Services";
    public string WorkApiUrl { get; set; } = "http://localhost:5002";
    public string GitHubApiUrl { get; set; } = "http://localhost:5003";
}

/// <summary>
/// Context provider that fetches project/issue data from Work Service
/// and code context from GitHub Service via HTTP.
/// 
/// CODE CONTEXT BRIDGE: Bu provider, GitHub Service'den dosya ağacı ve
/// kritik dosya içeriklerini çekerek AI'a code context sağlar.
/// </summary>
public class HttpWorkContextProvider : IContextProvider
{
    private readonly HttpClient _httpClient;
    private readonly WorkServiceOptions _options;
    private readonly ILogger<HttpWorkContextProvider> _logger;

    public string SourceName => "WorkService";
    public int Priority => 1;

    public HttpWorkContextProvider(
        HttpClient httpClient,
        IOptions<WorkServiceOptions> options,
        ILogger<HttpWorkContextProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiContext> GetContextAsync(Guid projectId, string issueKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching context for Project={ProjectId}, Issue={IssueKey}", projectId, issueKey);

        // Fetch project details
        var project = await FetchProjectAsync(projectId, cancellationToken);

        // Fetch issue details
        var issue = await FetchIssueAsync(issueKey, cancellationToken);

        // CODE CONTEXT BRIDGE: GitHub Service'den dosya ağacı ve kritik dosyalar çek
        var (fileTreeStructure, sourceFiles) = await FetchCodeContextAsync(projectId, cancellationToken);

        return new AiContext
        {
            Project = project,
            Issue = issue,
            SourceFiles = sourceFiles,
            FileTreeStructure = fileTreeStructure
        };
    }

    /// <summary>
    /// GitHub Service'den recursive dosya ağacı ve kritik dosya içerikleri çeker.
    /// Hata durumunda boş context döner (fallback — GitHub bağlantısı olmayabilir).
    /// </summary>
    private async Task<(string? FileTree, List<CodeFileDto> SourceFiles)> FetchCodeContextAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching code context from GitHub Service for project {ProjectId}", projectId);

            // 1. Repo analiz endpoint'ini çağır
            var analyzeUrl = $"{_options.GitHubApiUrl}/api/repositories/{projectId}/analyze";
            var response = await _httpClient.GetAsync(analyzeUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "GitHub code context unavailable for project {ProjectId}: {StatusCode}. Continuing without code context.",
                    projectId, response.StatusCode);
                return (null, []);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var analysis = await response.Content.ReadFromJsonAsync<RepoAnalysisResponse>(options, cancellationToken);

            if (analysis == null)
            {
                _logger.LogWarning("GitHub analysis returned null for project {ProjectId}", projectId);
                return (null, []);
            }

            // 2. File tree'yi string formatına dönüştür
            var fileTree = analysis.TreePaths?.Length > 0
                ? string.Join("\n", analysis.TreePaths)
                : null;

            // 3. Kritik dosyaları CodeFileDto listesine dönüştür
            var sourceFiles = new List<CodeFileDto>();
            if (analysis.CriticalFiles != null)
            {
                foreach (var (filePath, content) in analysis.CriticalFiles)
                {
                    sourceFiles.Add(new CodeFileDto
                    {
                        Path = filePath,
                        Content = content,
                        Language = DetectLanguage(filePath)
                    });
                }
            }

            _logger.LogInformation(
                "GitHub code context populated for {ProjectId}: {TreeCount} tree items, {FileCount} source files, TechStack=[{TechStack}]",
                projectId,
                analysis.TreePaths?.Length ?? 0,
                sourceFiles.Count,
                string.Join(", ", analysis.DetectedTechStack ?? []));

            return (fileTree, sourceFiles);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "GitHub Service unreachable for project {ProjectId}. Continuing without code context.",
                projectId);
            return (null, []);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex,
                "GitHub Service request timed out for project {ProjectId}. Continuing without code context.",
                projectId);
            return (null, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error fetching code context for project {ProjectId}. Continuing without code context.",
                projectId);
            return (null, []);
        }
    }

    /// <summary>
    /// AI'ın Multi-Turn pattern'da istediği dosyaları GitHub Service'den çeker.
    /// GenerateAiPlanCommandHandler tarafından Phase 2'de kullanılır.
    /// </summary>
    public async Task<Dictionary<string, string>> FetchRequestedFilesAsync(
        Guid projectId, string[] filePaths, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching {Count} requested files from GitHub Service for project {ProjectId}: [{Files}]",
                filePaths.Length, projectId, string.Join(", ", filePaths.Take(5)));

            var contentsUrl = $"{_options.GitHubApiUrl}/api/repositories/{projectId}/contents";
            var requestBody = new { FilePaths = filePaths };
            var response = await _httpClient.PostAsJsonAsync(contentsUrl, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch requested files: {StatusCode}", response.StatusCode);
                return new Dictionary<string, string>();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var files = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(options, cancellationToken);

            _logger.LogInformation("Successfully fetched {Count}/{Total} requested files", files?.Count ?? 0, filePaths.Length);
            return files ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching requested files for project {ProjectId}", projectId);
            return new Dictionary<string, string>();
        }
    }

    private async Task<ProjectDto> FetchProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_options.WorkApiUrl}/api/projects/{projectId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (response.IsSuccessStatusCode)
            {
                var project = await response.Content.ReadFromJsonAsync<WorkServiceProjectResponse>(options, cancellationToken);
                if (project != null)
                {
                    return new ProjectDto
                    {
                        Id = project.Id,
                        Key = project.Key,
                        Name = project.Name,
                        Description = project.Description,
                        TechStack = project.TechStack ?? [],
                        RepositoryUrl = project.RepositoryUrl
                    };
                }
            }
            
            _logger.LogWarning("Failed to fetch project {ProjectId}: {StatusCode}", projectId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project {ProjectId}", projectId);
        }

        // Return placeholder if fetch fails
        return new ProjectDto
        {
            Id = projectId,
            Key = "UNKNOWN",
            Name = "Unknown Project"
        };
    }

    private async Task<IssueDto> FetchIssueAsync(string issueKey, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_options.WorkApiUrl}/api/issues/{issueKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            if (response.IsSuccessStatusCode)
            {
                var issue = await response.Content.ReadFromJsonAsync<WorkServiceIssueResponse>(options, cancellationToken);
                if (issue != null)
                {
                    return new IssueDto
                    {
                        Id = issue.Id,
                        Key = issue.Key,
                        Title = issue.Title,
                        Description = issue.Description,
                        Type = issue.Type?.ToString() ?? "Unknown",
                        Priority = issue.Priority?.ToString() ?? "Unknown",
                        Status = issue.Status?.ToString() ?? "Unknown"
                    };
                }
            }

            _logger.LogWarning("Failed to fetch issue {IssueKey}: {StatusCode}", issueKey, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issue {IssueKey}", issueKey);
        }

        // Return placeholder if fetch fails
        return new IssueDto
        {
            Id = Guid.Empty,
            Key = issueKey,
            Title = "Unknown Issue"
        };
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.WorkApiUrl}/health";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? DetectLanguage(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".ts" => "typescript",
            ".tsx" => "tsx",
            ".js" => "javascript",
            ".jsx" => "jsx",
            ".json" => "json",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            ".sql" => "sql",
            ".py" => "python",
            ".java" => "java",
            ".go" => "go",
            _ => null
        };
    }
}

#region Response Models

internal class WorkServiceProjectResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<string>? TechStack { get; set; }
    public string? RepositoryUrl { get; set; }
}

internal class WorkServiceIssueResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public object? Type { get; set; }
    public object? Priority { get; set; }
    public object? Status { get; set; }
}

/// <summary>
/// GitHub Service /api/repositories/{id}/analyze response model
/// </summary>
internal class RepoAnalysisResponse
{
    public string[]? TreePaths { get; set; }
    public Dictionary<string, string>? CriticalFiles { get; set; }
    public string[]? DetectedTechStack { get; set; }
    public string[]? ExistingWorkflows { get; set; }
}

#endregion

