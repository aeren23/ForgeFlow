using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue atama handler
/// </summary>
public class AssignIssueHandler : IRequestHandler<AssignIssueCommand, AssignIssueResult>
{
    private readonly IWorkDbContext _context;

    public AssignIssueHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<AssignIssueResult> Handle(AssignIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.Key}' not found");

        var oldAssigneeId = issue.AssigneeId;
        issue.AssigneeId = request.AssigneeId;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new AssignIssueResult(issue.Key, oldAssigneeId, issue.AssigneeId);
    }
}
