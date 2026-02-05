using ForgeFlow.GitHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.GitHub.Api.Controllers;

/// <summary>
/// Repository işlemleri - AI Code Context için tree ve contents endpoint'leri
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
    /// Proje ağacını döndür (AI Orchestrator kullanacak)
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
    /// Belirtilen dosyaların içeriklerini döndür
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
}

public record GetFilesRequest(string[] FilePaths);
