using ForgeFlow.Identity.Application.Users.Queries;
using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Application.Users.Handlers;

/// <summary>
/// Kullanıcı arama ve listeleme handler'ı
/// </summary>
public class ListUsersHandler : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ListUsersHandler> _logger;

    public ListUsersHandler(UserManager<ApplicationUser> userManager, ILogger<ListUsersHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ListUsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        // Search logic
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(term))
            );
        }

        // Total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Pagination
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.UserName ?? "",
                u.Email ?? "",
                u.FullName
            ))
            .ToListAsync(cancellationToken);

        return new ListUsersResult(users, totalCount);
    }
}
