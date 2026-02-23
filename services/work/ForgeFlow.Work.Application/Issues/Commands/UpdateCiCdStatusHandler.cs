using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Abstractions;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// CI/CD pipeline durumunu Issue entity'sine kaydeder ve CiCdStatusUpdated event'i publish eder.
/// İdempotent: Aynı RunId ile tekrar çağrılabilir, sadece güncelleme yapar.
/// </summary>
public class UpdateCiCdStatusHandler : IRequestHandler<UpdateCiCdStatusCommand, UpdateCiCdStatusResult>
{
    private readonly IWorkDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateCiCdStatusHandler(IWorkDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<UpdateCiCdStatusResult> Handle(UpdateCiCdStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _context.Issues
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Key == request.IssueKey.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.IssueKey}' not found");

        // CI/CD alanlarını güncelle
        issue.CiCdStatus = request.Status;
        issue.CiCdWorkflowName = request.WorkflowName;
        issue.CiCdRunUrl = request.HtmlUrl;
        issue.CiCdUpdatedAtUtc = DateTime.UtcNow;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notification Service'e real-time güncelleme için bildir
        try
        {
            await _publishEndpoint.Publish(new CiCdStatusUpdated(
                IssueKey: issue.Key,
                ProjectId: issue.Project.Id,
                WorkflowName: request.WorkflowName,
                Status: request.Status,
                HtmlUrl: request.HtmlUrl,
                Timestamp: DateTime.UtcNow
            ), cancellationToken);
        }
        catch
        {
            // Event publish başarısız olsa bile CI/CD status güncellemeyi geri alma
        }

        return new UpdateCiCdStatusResult(issue.Key, issue.Project.Id, request.Status);
    }
}
