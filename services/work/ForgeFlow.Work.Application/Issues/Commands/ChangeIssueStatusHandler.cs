using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue status change handler - includes permission checks and publishes events for real-time updates
/// </summary>
public class ChangeIssueStatusHandler : IRequestHandler<ChangeIssueStatusCommand, ChangeIssueStatusResult>
{
    private readonly IWorkDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IProjectPermissionService _permissionService;

    public ChangeIssueStatusHandler(
        IWorkDbContext context,
        IPublishEndpoint publishEndpoint,
        IProjectPermissionService permissionService)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _permissionService = permissionService;
    }

    public async Task<ChangeIssueStatusResult> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .Include(i => i.Project)
                .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.Key}' not found");

        // Permission Check
        var userId = request.UserId ?? throw new UnauthorizedAccessException("User ID is required for status transitions");
        var member = issue.Project.Members.FirstOrDefault(m => m.UserId == userId);
        var role = member?.Role ?? ProjectRole.Viewer;

        if (!_permissionService.CanTransitionIssue(role, issue.Status, request.NewStatus, issue.AssigneeId, userId))
        {
            throw new UnauthorizedAccessException("You don't have permission to perform this status transition.");
        }

        var oldStatus = issue.Status;
        issue.Status = request.NewStatus;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        // Track StartedAtUtc when moving to InProgress
        if (request.NewStatus == IssueStatus.InProgress && oldStatus == IssueStatus.Open)
        {
            issue.StartedAtUtc = DateTime.UtcNow;
        }

        // Set ClosedAtUtc when closing
        if (request.NewStatus == IssueStatus.Closed)
        {
            issue.ClosedAtUtc = DateTime.UtcNow;
        }
        else if (oldStatus == IssueStatus.Closed)
        {
            // Clear closure date if reopened
            issue.ClosedAtUtc = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Publish IssueStatusChanged event for real-time board updates
        try
        {
            await _publishEndpoint.Publish(new IssueStatusChanged(
                IssueKey: issue.Key,
                ProjectId: issue.Project.Id,
                OldStatus: (int)oldStatus,
                NewStatus: (int)issue.Status,
                UpdatedByUserId: userId,
                Timestamp: DateTime.UtcNow
            ), cancellationToken);
        }
        catch
        {
            // Don't fail the status change if event publishing fails
        }

        return new ChangeIssueStatusResult(issue.Key, oldStatus, issue.Status);
    }
}
