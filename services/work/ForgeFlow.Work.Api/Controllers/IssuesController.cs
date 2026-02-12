using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Api.Services;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Issues.Commands;
using ForgeFlow.Work.Application.Issues.Queries;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Work.Api.Controllers;

/// <summary>
/// Issue yönetimi controller'ı
/// </summary>
[ApiController]
[Route("api/issues")]
public class IssuesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publish;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(
        IMediator mediator,
        IPublishEndpoint publish,
        ICurrentUserService currentUser,
        ILogger<IssuesController> logger)
    {
        _mediator = mediator;
        _publish = publish;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Issue listesi
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListIssuesResult>> List(
        [FromQuery] string? projectKey = null,
        [FromQuery] IssueStatus? status = null,
        [FromQuery] IssuePriority? priority = null,
        [FromQuery] IssueType? type = null,
        [FromQuery] string? assigneeId = null,
        [FromQuery] Guid? parentIssueId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new ListIssuesQuery(
            ProjectKey: projectKey,
            Status: status,
            Priority: priority,
            Type: type,
            AssigneeId: assigneeId,
            ReporterId: null,
            ParentIssueId: parentIssueId,
            Page: page,
            PageSize: pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Tek issue getir
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<IssueDto>> Get(string key)
    {
        var result = await _mediator.Send(new GetIssueQuery(key));
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Yeni issue oluştur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateIssueResult>> Create([FromBody] CreateIssueRequest request)
    {
        var command = new CreateIssueCommand(
            request.ProjectKey,
            request.Title,
            request.Description,
            request.Type,
            request.Priority,
            request.ParentIssueKey,
            request.AssigneeId,
            request.DueDate,
            request.EstimatedHours,
            _currentUser.UserId ?? "anonymous"
        );

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { key = result.Key }, result);
    }

    /// <summary>
    /// Issue güncelle
    /// </summary>
    [HttpPut("{key}")]
    public async Task<ActionResult<UpdateIssueResult>> Update(string key, [FromBody] UpdateIssueRequest request)
    {
        var command = new UpdateIssueCommand(
            key,
            request.Title,
            request.Description,
            request.Type,
            request.Priority,
            request.AssigneeId,
            request.DueDate,
            request.EstimatedHours
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Issue status değiştir
    /// </summary>
    [HttpPost("{key}/status")]
    public async Task<ActionResult<ChangeIssueStatusResult>> ChangeStatus(string key, [FromBody] ChangeStatusRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ChangeIssueStatusCommand(key, request.Status, _currentUser.UserId));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Issue ata - Only Admin/Owner/TechLead can assign
    /// </summary>
    [HttpPost("{key}/assign")]
    public async Task<ActionResult<AssignIssueResult>> Assign(string key, [FromBody] AssignIssueRequest request)
    {
        try
        {
            var result = await _mediator.Send(new AssignIssueCommand(key, request.AssigneeId, _currentUser.UserId, request.CreateBranch));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    /// <summary>
    /// AI plan oluştur - Issue için AI destekli plan üretimi başlat
    /// </summary>
    [HttpPost("{issueKey}/generate")]
    public async Task<IActionResult> Generate(string issueKey, [FromBody] GeneratePlanRequest? request = null)
    {
        request ??= new GeneratePlanRequest();

        // Get issue to find project ID
        // Get issue to find project ID
        var issue = await _mediator.Send(new GetIssueQuery(issueKey));
        if (issue == null)
            return NotFound(new { error = $"Issue '{issueKey}' not found" });

        var correlationId = Guid.NewGuid().ToString("N");
        var userId = _currentUser.UserId ?? "anonymous";

        var evt = new EventEnvelope<AiPlanRequested>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: correlationId,
            UserId: userId,
            CausationId: null,
            Data: new AiPlanRequested(
                IssueId: issueKey,
                ProjectId: issue.ProjectId.ToString(),
                RequestedByUserId: userId,
                BundleType: request.BundleType ?? "FullStack",
                Strictness: request.Strictness ?? "Normal",
                PreferredProvider: request.PreferredProvider
            )
        );

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId,
            ["IssueKey"] = issueKey
        }))
        {
            _logger.LogInformation(
                "Published AiPlanRequested | IssueKey={IssueKey} Provider={Provider} BundleType={BundleType}",
                issueKey, request.PreferredProvider ?? "Default", request.BundleType ?? "FullStack");
        }

        await _publish.Publish(evt);

        return Accepted(new
        {
            correlationId,
            issueKey,
            userId,
            provider = request.PreferredProvider ?? "Default",
            bundleType = request.BundleType ?? "FullStack",
            message = "AI plan generation started. Check Seq logs for progress."
        });
    }
}

// Request DTOs
public record CreateIssueRequest(
    string ProjectKey,
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    string? ParentIssueKey,
    string? AssigneeId,
    DateTime? DueDate,
    decimal? EstimatedHours
);

public record UpdateIssueRequest(
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    string? AssigneeId,
    DateTime? DueDate,
    decimal? EstimatedHours
);

public record ChangeStatusRequest(IssueStatus Status);
public record AssignIssueRequest(string? AssigneeId, bool CreateBranch = true);

/// <summary>
/// Request for AI plan generation
/// </summary>
public record GeneratePlanRequest(
    string? BundleType = "FullStack",      // "FullStack", "Backend", "Frontend"
    string? Strictness = "Normal",          // "Strict", "Normal", "Flexible"
    string? PreferredProvider = null        // "Gemini", "Groq", etc. Null = use default
);
