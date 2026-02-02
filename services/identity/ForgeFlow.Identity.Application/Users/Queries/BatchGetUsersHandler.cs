using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Identity.Application.Users.Queries;

public class BatchGetUsersHandler : IRequestHandler<BatchGetUsersQuery, List<UserDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public BatchGetUsersHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<UserDto>> Handle(BatchGetUsersQuery request, CancellationToken cancellationToken)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return new List<UserDto>();
        }

        // UserManager does not have a direct WhereIn, so we use the Queryable extension if available,
        // or just direct EF usage if we had the context (but here we are in Application layer dependent on Identity).
        // Best approach with UserManager is usually direct query on Users IQueryable.

        var users = await _userManager.Users
            .Where(u => request.UserIds.Contains(u.Id))
            .Select(u => new UserDto(
                u.Id,
                u.UserName!,
                u.Email!,
                u.FullName
            ))
            .ToListAsync(cancellationToken);

        return users;
    }
}
