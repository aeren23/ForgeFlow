using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Queries;

/// <summary>
/// Issue getirme handler
/// </summary>
public class GetIssueHandler : IRequestHandler<GetIssueQuery, IssueDto?>
{
    private readonly IWorkDbContext _context;

    public GetIssueHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<IssueDto?> Handle(GetIssueQuery request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .Include(i => i.Project)
            .Include(i => i.ParentIssue)
            .Include(i => i.ChildIssues)
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken);

        if (issue == null)
            return null;

        return new IssueDto(
            issue.Id,
            issue.Key,
            issue.Title,
            issue.Description,
            issue.Status,
            issue.Priority,
            issue.Type,
            issue.ProjectId,
            issue.Project.Key,
            issue.ParentIssue?.Key,
            issue.ReporterId,
            issue.AssigneeId,
            issue.DueDate,
            issue.EstimatedHours,
            issue.CreatedAtUtc,
            issue.UpdatedAtUtc,
            issue.ClosedAtUtc,
            issue.StartedAtUtc,
            issue.BranchName,
            issue.ChildIssues.Count
        );
    }
}
