using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue status değiştirme komutu
/// </summary>
public record ChangeIssueStatusCommand(
    string Key,
    IssueStatus NewStatus,
    string? UserId = null,
    bool IsSystemAction = false
) : IRequest<ChangeIssueStatusResult>;

public record ChangeIssueStatusResult(
    string Key,
    IssueStatus OldStatus,
    IssueStatus NewStatus
);
