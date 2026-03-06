using ForgeFlow.GitHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.GitHub.Api.Controllers;

/// <summary>
/// Repository işlemleri - AI Code Context için tree, contents ve analyze endpoint'leri
/// </summary>
[ApiController]
[Route("api/repositories")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryContentService _contentService;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(
        IRepositoryContentService contentService,
        ILogger<RepositoriesController> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    /// <summary>
    /// Proje ağacını döndür (shallow - sadece belirtilen dizin)
    /// </summary>
    [HttpGet("{projectId}/tree")]
    public async Task<IActionResult> GetRepositoryTree(Guid projectId, [FromQuery] string? path = null)
    {
        try
        {
            var tree = await _contentService.GetTreeAsync(projectId, path);
            return Ok(tree);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Repository not found for project {ProjectId}", projectId);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Recursive dosya ağacını döndür (Git Trees API, binary filtrelenmiş, max limit)
    /// AI Orchestrator Multi-Turn pattern'da kullanılır
    /// </summary>
    [HttpGet("{projectId}/tree/recursive")]
    public async Task<IActionResult> GetRecursiveTree(Guid projectId, [FromQuery] int maxItems = 500)
    {
        try
        {
            var tree = await _contentService.GetRecursiveTreeAsync(projectId, maxItems);
            return Ok(tree);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Repository not found for project {ProjectId}", projectId);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Belirtilen dosyaların içeriklerini döndür
    /// AI Multi-Turn pattern'da AI'ın istediği dosyalar için kullanılır
    /// </summary>
    [HttpPost("{projectId}/contents")]
    public async Task<IActionResult> GetFileContents(Guid projectId, [FromBody] GetFilesRequest request)
    {
        if (request.FilePaths == null || request.FilePaths.Length == 0)
            return BadRequest(new { error = "FilePaths cannot be empty" });

        try
        {
            var files = await _contentService.GetFilesAsync(projectId, request.FilePaths);
            return Ok(files);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Repository not found for project {ProjectId}", projectId);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Repo'yu analiz et: recursive tree + kritik dosya içerikleri + tech stack tespiti
    /// AI Orchestrator Code Context Bridge ve Workflow Generation için kullanılır
    /// </summary>
    [HttpGet("{projectId}/analyze")]
    public async Task<IActionResult> AnalyzeRepository(Guid projectId)
    {
        try
        {
            _logger.LogInformation("Analyzing repository for project {ProjectId}", projectId);
            var result = await _contentService.AnalyzeRepoAsync(projectId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Repository not found for project {ProjectId}", projectId);
            return NotFound(new { error = ex.Message });
        }
    }
}

public record GetFilesRequest(string[] FilePaths);

