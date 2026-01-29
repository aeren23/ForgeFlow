using System.Net.Http.Json;
using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForgeFlow.AiOrchestrator.Infrastructure.ContextProviders;

/// <summary>
/// Configuration options for Work Service client
/// </summary>
public class WorkServiceOptions
{
    public const string SectionName = "Services";
    public string WorkApiUrl { get; set; } = "http://localhost:5002";
}

/// <summary>
/// Context provider that fetches project/issue data from Work Service via HTTP
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

        return new AiContext
        {
            Project = project,
            Issue = issue,
            SourceFiles = [], // Will be populated by GitHub provider in the future
            FileTreeStructure = null
        };
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
}

#region Work Service Response Models

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

#endregion
