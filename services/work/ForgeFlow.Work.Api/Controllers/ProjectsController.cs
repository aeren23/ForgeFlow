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

    /// <summary>
    /// Yapay zeka ile plan oluştur (Epic + AI Trigger)
    /// </summary>
    [HttpPost("{key}/generate-plan")]
    public async Task<ActionResult> GenerateAiPlan(string key, [FromBody] GenerateAiPlanRequest request)
    {
        // 1. Önce Epic oluştur (Plan Konteyneri)
        var createEpicCommand = new ForgeFlow.Work.Application.Issues.Commands.CreateIssueCommand(
            ProjectKey: key,
            Title: request.PlanName,
            Description: request.Description,
            Type: IssueType.Epic,
            Priority: IssuePriority.High,
            ParentIssueKey: null,
            AssigneeId: _currentUser.UserId,
            DueDate: DateTime.UtcNow.AddDays(7),
            EstimatedHours: null,
            ReporterId: _currentUser.UserId!
        );

        var epicResult = await _mediator.Send(createEpicCommand);

        // 2. AI Plan Command'i tetikle (Bu command Orchestrator'a gidecek)
        // Not: Bu command Work servisinde tanımlı olmayabilir, Orchestrator'a HTTP veya Masstransit ile gitmeliyiz.
        // Ancak HttpWorkContextProvider kullandığımız için AI servisimiz Work servisini biliyor.
        // Ama buradan AI servisine nasıl ulaşacağız?
        // Çözüm: Work servisi bir "Event" fırlatabilir (AiPlanRequested) veya direkt AI servisine istek atabilir.
        // Mevcut mimaride: Gateway -> Orchestrator(GenerateAiPlanCommand) -> AI -> Event -> Consumer
        // Yani bizim buradan AI servisine ulaşmamız lazım.
        // Kestirme yol: Biz burada sadece Epic oluşturduk. Frontend'e Epic Key dönelim.
        // Frontend, mevcut "generateAiPlan" metodunu bu Epic Key ile çağırsın.
        // Bu sayede Orchestrator'a giden yol değişmez.

        return Ok(new { EpicKey = epicResult.Key, EpicId = epicResult.Id });
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

public record GenerateAiPlanRequest(
    string PlanName,
    string Description,
    string BundleType = "FullStack"
);
