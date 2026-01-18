using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ForgeFlow.Identity.Application.Roles.Queries;

public class GetUserRolesHandler : IRequestHandler<GetUserRolesQuery, GetUserRolesResult>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserRolesHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<GetUserRolesResult> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);

        if (user == null)
        {
            return new GetUserRolesResult(false, null, Enumerable.Empty<string>(), "Kullanıcı bulunamadı.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new GetUserRolesResult(true, user.Id, roles, null);
    }
}
