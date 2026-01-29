using ForgeFlow.Work.Api.Services;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Projects.Commands;
using ForgeFlow.Work.Application.Projects.Queries;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Work.Api.Controllers;

/// <summary>
/// Proje yönetimi controller'ı
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Proje listesi
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListProjectsResult>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = true)
    {
        var result = await _mediator.Send(new ListProjectsQuery(page, pageSize, isActive));
        return Ok(result);
    }

    /// <summary>
    /// Tek proje getir
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<ProjectDto>> Get(string key)
    {
        var result = await _mediator.Send(new GetProjectQuery(key));
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Yeni proje oluştur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateProjectResult>> Create([FromBody] CreateProjectRequest request)
    {
        var command = new CreateProjectCommand(
            request.Key,
            request.Name,
            request.Description,
            request.RepositoryUrl,
            request.RepositoryProvider,
            request.TechStack ?? [],
            request.ProjectType,
            _currentUser.UserId ?? "anonymous"
        );

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { key = result.Key }, result);
    }

    /// <summary>
    /// Proje güncelle
    /// </summary>
    [HttpPut("{key}")]
    public async Task<ActionResult<UpdateProjectResult>> Update(string key, [FromBody] UpdateProjectRequest request)
    {
        var command = new UpdateProjectCommand(
            key,
            request.Name,
            request.Description,
            request.RepositoryUrl,
            request.RepositoryProvider,
            request.DefaultBranch ?? "main",
            request.TechStack ?? [],
            request.ProjectType
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Projeye üye ekle
    /// </summary>
    [HttpPost("{key}/members")]
    public async Task<ActionResult> AddMember(string key, [FromBody] AddMemberRequest request)
    {
        var command = new AddProjectMemberCommand(key, request.UserId, request.Role ?? "Member");
        var result = await _mediator.Send(command);
        if (!result) return NotFound();
        return Ok();
    }
}

public record AddMemberRequest(string UserId, string? Role);

// Request DTOs
public record CreateProjectRequest(
    string Key,
    string Name,
    string? Description,
    string? RepositoryUrl,
    RepositoryProvider? RepositoryProvider,
    string[]? TechStack,
    ProjectType ProjectType
);

public record UpdateProjectRequest(
    string Name,
    string? Description,
    string? RepositoryUrl,
    RepositoryProvider? RepositoryProvider,
    string? DefaultBranch,
    string[]? TechStack,
    ProjectType ProjectType
);
