using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Queries;

/// <summary>
/// Proje listeleme handler
/// </summary>
public class ListProjectsHandler : IRequestHandler<ListProjectsQuery, ListProjectsResult>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ListProjectsHandler(IWorkDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ListProjectsResult> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Projects.Include(p => p.Members).AsQueryable();

        // 1. Authorization Filter (Hybrid)
        // Admin: Hepsini görür
        // Diğerleri: Creator olduğu VEYA Üye olduğu projeleri görür
        if (!_currentUser.IsInRole("Admin"))
        {
            var userId = _currentUser.UserId;
            query = query.Where(p =>
                p.CreatorId == userId ||
                p.Members.Any(m => m.UserId == userId));
        }

        // IsActive filter
        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        // Total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Pagination
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.Key,
                p.Name,
                p.Description,
                p.TechStack,
                p.Issues.Count,
                p.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new ListProjectsResult(items, totalCount, request.Page, request.PageSize);
    }
}
