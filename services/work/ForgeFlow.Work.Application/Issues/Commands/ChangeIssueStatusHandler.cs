using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue status değiştirme handler - now publishes IssueStatusChanged event for real-time updates
/// </summary>
public class ChangeIssueStatusHandler : IRequestHandler<ChangeIssueStatusCommand, ChangeIssueStatusResult>
{
    private readonly IWorkDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public ChangeIssueStatusHandler(IWorkDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ChangeIssueStatusResult> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .Include(i => i.Project)
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

        // Publish IssueStatusChanged event for real-time board updates
        try
        {
            await _publishEndpoint.Publish(new IssueStatusChanged(
                IssueKey: issue.Key,
                ProjectId: issue.Project.Id,
                OldStatus: (int)oldStatus,
                NewStatus: (int)issue.Status,
                UpdatedByUserId: request.UserId ?? "system",
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
