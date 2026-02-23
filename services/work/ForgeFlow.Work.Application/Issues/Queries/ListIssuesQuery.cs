using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Issues.Queries;

/// <summary>
/// Issue listeleme sorgusu
/// </summary>
public record ListIssuesQuery(
    string? ProjectKey = null,
    IssueStatus? Status = null,
    IssuePriority? Priority = null,
    IssueType? Type = null,
    string? AssigneeId = null,
    string? ReporterId = null,
    Guid? ParentIssueId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<ListIssuesResult>;

public record ListIssuesResult(
    IReadOnlyList<IssueListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record IssueListItemDto(
    Guid Id,
    string Key,
    string Title,
    string? Description,
    IssueStatus Status,
    IssuePriority Priority,
    IssueType Type,
    Guid ProjectId,
    string ProjectKey,
    string? AssigneeId,
    DateTime? DueDate,
    DateTime CreatedAtUtc,
    Guid? ParentIssueId,
    string? BranchName,
    DateTime? StartedAtUtc,
    string? CiCdStatus,
    string? CiCdRunUrl
);
