using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ForgeFlow.Identity.Application.Auth.Queries;

public class GetProfileHandler : IRequestHandler<GetProfileQuery, GetProfileResult>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetProfileHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<GetProfileResult> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new GetProfileResult(
            Id: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: roles,
            CreatedAt: user.CreatedAtUtc
        );
    }
}
