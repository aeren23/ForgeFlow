using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue atama komutu - Only Admin/Owner/TechLead can use
/// </summary>
public record AssignIssueCommand(
    string Key,
    string? AssigneeId,
    string? UserId
) : IRequest<AssignIssueResult>;

public record AssignIssueResult(
    string Key,
    string? OldAssigneeId,
    string? NewAssigneeId
);
