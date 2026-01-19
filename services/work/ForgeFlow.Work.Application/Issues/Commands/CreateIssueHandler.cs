using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue oluşturma handler
/// </summary>
public class CreateIssueHandler : IRequestHandler<CreateIssueCommand, CreateIssueResult>
{
    private readonly IWorkDbContext _context;

    public CreateIssueHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<CreateIssueResult> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        // Proje'yi bul ve issue numarasını artır
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Key == request.ProjectKey.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Project '{request.ProjectKey}' not found");

        // Parent issue kontrolü
        Guid? parentIssueId = null;
        if (!string.IsNullOrEmpty(request.ParentIssueKey))
        {
            var parentIssue = await _context.Issues
                .FirstOrDefaultAsync(i => i.Key == request.ParentIssueKey.ToUpperInvariant(), cancellationToken)
                ?? throw new InvalidOperationException($"Parent issue '{request.ParentIssueKey}' not found");
            parentIssueId = parentIssue.Id;
        }

        // Issue key oluştur (atomic increment)
        var issueNumber = project.NextIssueNumber;
        project.NextIssueNumber++;
        var issueKey = $"{project.Key}-{issueNumber}";

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            Key = issueKey,
            Title = request.Title,
            Description = request.Description,
            Status = IssueStatus.Open,
            Priority = request.Priority,
            Type = request.Type,
            ProjectId = project.Id,
            ParentIssueId = parentIssueId,
            ReporterId = request.ReporterId,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateIssueResult(issue.Id, issue.Key, issue.Title, issue.Status);
    }
}
