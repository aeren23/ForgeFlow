using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue güncelleme komutu
/// </summary>
public record UpdateIssueCommand(
    string Key,
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    string? AssigneeId,
    DateTime? DueDate,
    decimal? EstimatedHours
) : IRequest<UpdateIssueResult>;

public record UpdateIssueResult(
    Guid Id,
    string Key,
    string Title
);
