using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Identity.Application.Roles.Queries;

public class GetRolesHandler : IRequestHandler<GetRolesQuery, GetRolesResult>
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public GetRolesHandler(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<GetRolesResult> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles
            .Select(r => new RoleDto(
                r.Id,
                r.Name ?? "",
                r.Description,
                r.IsSystem,
                r.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new GetRolesResult(roles);
    }
}
