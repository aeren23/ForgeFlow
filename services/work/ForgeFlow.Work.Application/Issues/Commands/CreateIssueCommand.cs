using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Yeni issue oluşturma komutu
/// </summary>
public record CreateIssueCommand(
    string ProjectKey,
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    string? ParentIssueKey,
    string? AssigneeId,
    DateTime? DueDate,
    decimal? EstimatedHours,
    string ReporterId
) : IRequest<CreateIssueResult>;

public record CreateIssueResult(
    Guid Id,
    string Key,
    string Title,
    IssueStatus Status
);
