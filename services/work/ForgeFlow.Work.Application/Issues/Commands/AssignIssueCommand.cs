using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue atama komutu
/// </summary>
public record AssignIssueCommand(
    string Key,
    string? AssigneeId
) : IRequest<AssignIssueResult>;

public record AssignIssueResult(
    string Key,
    string? OldAssigneeId,
    string? NewAssigneeId
);
