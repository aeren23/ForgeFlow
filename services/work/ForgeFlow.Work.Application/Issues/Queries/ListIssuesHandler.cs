using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Queries;

/// <summary>
/// Issue listeleme handler
/// </summary>
public class ListIssuesHandler : IRequestHandler<ListIssuesQuery, ListIssuesResult>
{
    private readonly IWorkDbContext _context;

    public ListIssuesHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<ListIssuesResult> Handle(ListIssuesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Issues
            .Include(i => i.Project)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(request.ProjectKey))
        {
            query = query.Where(i => i.Project.Key == request.ProjectKey.ToUpperInvariant());
        }

        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(i => i.Priority == request.Priority.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(i => i.Type == request.Type.Value);
        }

        if (!string.IsNullOrEmpty(request.AssigneeId))
        {
            query = query.Where(i => i.AssigneeId == request.AssigneeId);
        }

        if (!string.IsNullOrEmpty(request.ReporterId))
        {
            query = query.Where(i => i.ReporterId == request.ReporterId);
        }

        // Total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Pagination
        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new IssueListItemDto(
                i.Id,
                i.Key,
                i.Title,
                i.Status,
                i.Priority,
                i.Type,
                i.Project.Key,
                i.AssigneeId,
                i.DueDate,
                i.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new ListIssuesResult(items, totalCount, request.Page, request.PageSize);
    }
}
