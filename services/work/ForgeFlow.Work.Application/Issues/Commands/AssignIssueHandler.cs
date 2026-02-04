using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue assignment handler - publishes IssueAssigned event for GitHub integration
/// Only Admin/Owner/TechLead can assign users to issues
/// </summary>
public class AssignIssueHandler : IRequestHandler<AssignIssueCommand, AssignIssueResult>
{
    private readonly IWorkDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IProjectPermissionService _permissionService;

    public AssignIssueHandler(
        IWorkDbContext context,
        IPublishEndpoint publishEndpoint,
        IProjectPermissionService permissionService)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _permissionService = permissionService;
    }

    public async Task<AssignIssueResult> Handle(AssignIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .Include(i => i.Project)
                .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.Key}' not found");

        // Permission Check - Only Admin/Owner/TechLead can assign
        var userId = request.UserId ?? throw new UnauthorizedAccessException("User ID is required for assignment");
        var member = issue.Project.Members.FirstOrDefault(m => m.UserId == userId);
        var role = member?.Role ?? ProjectRole.Viewer;

        if (!_permissionService.CanAssignUser(role, request.AssigneeId ?? "", userId))
        {
            throw new UnauthorizedAccessException("Only Admin, Owner, or TechLead can assign issues.");
        }

        var oldAssigneeId = issue.AssigneeId;
        issue.AssigneeId = request.AssigneeId;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        // Auto-transition: If issue is Open and we're assigning someone, move to InProgress
        if (request.AssigneeId != null && issue.Status == IssueStatus.Open)
        {
            issue.Status = IssueStatus.InProgress;
            issue.StartedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Publish IssueAssigned event (GitHub Service will consume this to create branch)
        if (request.AssigneeId != null)
        {
            try
            {
                await _publishEndpoint.Publish(new IssueAssigned(
                    IssueKey: issue.Key,
                    IssueTitle: issue.Title,
                    ProjectId: issue.ProjectId,
                    OldAssigneeId: oldAssigneeId,
                    NewAssigneeId: request.AssigneeId,
                    RepositoryUrl: issue.Project.RepositoryUrl,
                    DefaultBranch: issue.Project.DefaultBranch,
                    Timestamp: DateTime.UtcNow
                ), cancellationToken);
            }
            catch
            {
                // Don't fail the assignment if event publishing fails
            }
        }

        return new AssignIssueResult(issue.Key, oldAssigneeId, issue.AssigneeId);
    }
}
