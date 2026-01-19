using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue status değiştirme handler
/// </summary>
public class ChangeIssueStatusHandler : IRequestHandler<ChangeIssueStatusCommand, ChangeIssueStatusResult>
{
    private readonly IWorkDbContext _context;

    public ChangeIssueStatusHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<ChangeIssueStatusResult> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.Key}' not found");

        var oldStatus = issue.Status;
        issue.Status = request.NewStatus;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        // Kapanış tarihi set et
        if (request.NewStatus == IssueStatus.Closed)
        {
            issue.ClosedAtUtc = DateTime.UtcNow;
        }
        else if (oldStatus == IssueStatus.Closed)
        {
            // Tekrar açıldıysa kapanış tarihini temizle
            issue.ClosedAtUtc = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ChangeIssueStatusResult(issue.Key, oldStatus, issue.Status);
    }
}
