using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue güncelleme handler
/// </summary>
public class UpdateIssueHandler : IRequestHandler<UpdateIssueCommand, UpdateIssueResult>
{
    private readonly IWorkDbContext _context;

    public UpdateIssueHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateIssueResult> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.Key}' not found");

        issue.Title = request.Title;
        issue.Description = request.Description;
        issue.Type = request.Type;
        issue.Priority = request.Priority;
        issue.AssigneeId = request.AssigneeId;
        issue.DueDate = request.DueDate;
        issue.EstimatedHours = request.EstimatedHours;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateIssueResult(issue.Id, issue.Key, issue.Title);
    }
}
