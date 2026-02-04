using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Issues.Queries;

/// <summary>
/// Tek issue getirme sorgusu
/// </summary>
public record GetIssueQuery(string Key) : IRequest<IssueDto?>;

/// <summary>
/// Issue DTO
/// </summary>
public record IssueDto(
    Guid Id,
    string Key,
    string Title,
    string? Description,
    IssueStatus Status,
    IssuePriority Priority,
    IssueType Type,
    Guid ProjectId,
    string ProjectKey,
    string? ParentIssueKey,
    string ReporterId,
    string? AssigneeId,
    DateTime? DueDate,
    decimal? EstimatedHours,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    DateTime? StartedAtUtc,
    string? BranchName,
    int ChildIssueCount
);
