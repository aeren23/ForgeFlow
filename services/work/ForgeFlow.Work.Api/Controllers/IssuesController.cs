using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Api.Services;
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new ListIssuesQuery(
            projectKey, status, priority, type, assigneeId, null, page, pageSize));
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
        var result = await _mediator.Send(new ChangeIssueStatusCommand(key, request.Status));
        return Ok(result);
    }

    /// <summary>
    /// Issue ata
    /// </summary>
    [HttpPost("{key}/assign")]
    public async Task<ActionResult<AssignIssueResult>> Assign(string key, [FromBody] AssignIssueRequest request)
    {
        var result = await _mediator.Send(new AssignIssueCommand(key, request.AssigneeId));
        return Ok(result);
    }

    /// <summary>
    /// AI plan oluştur (mevcut endpoint)
    /// </summary>
    [HttpPost("{issueId}/generate")]
    public async Task<IActionResult> Generate(string issueId)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var userId = _currentUser.UserId ?? "anonymous";

        var evt = new EventEnvelope<AiPlanRequested>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: correlationId,
            UserId: userId,
            CausationId: null,
            Data: new AiPlanRequested(
                IssueId: issueId,
                ProjectId: "PRJ-1",
                RequestedByUserId: userId,
                BundleType: "BUNDLE_V1",
                Strictness: "STANDARD"
            )
        );

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId
        }))
        {
            _logger.LogInformation(
                "Published AiPlanRequested | IssueId={IssueId}",
                issueId);
        }

        await _publish.Publish(evt);

        return Ok(new { correlationId, issueId, userId });
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
public record AssignIssueRequest(string? AssigneeId);
