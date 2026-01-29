namespace ForgeFlow.AiOrchestrator.Domain.Models;

/// <summary>
/// Aggregated context for AI plan generation.
/// This model is designed to be extended when GitHub integration is added.
/// </summary>
public class AiContext
{
    /// <summary>
    /// Project information
    /// </summary>
    public required ProjectDto Project { get; set; }

    /// <summary>
    /// Issue/Task information
    /// </summary>
    public required IssueDto Issue { get; set; }

    /// <summary>
    /// Source code files (populated by GitHub context provider in future)
    /// </summary>
    public List<CodeFileDto> SourceFiles { get; set; } = [];

    /// <summary>
    /// File tree structure of the repository (populated by GitHub)
    /// </summary>
    public string? FileTreeStructure { get; set; }

    /// <summary>
    /// Indicates if code context is available (GitHub integration required)
    /// </summary>
    public bool HasCodeContext => SourceFiles.Count > 0;
}

/// <summary>
/// Project information DTO
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> TechStack { get; set; } = [];
    public string? RepositoryUrl { get; set; }
}

/// <summary>
/// Issue/Task information DTO
/// </summary>
public class IssueDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Code file content DTO (for GitHub integration)
/// </summary>
public class CodeFileDto
{
    public required string Path { get; set; }
    public required string Content { get; set; }
    public string? Language { get; set; }
}
