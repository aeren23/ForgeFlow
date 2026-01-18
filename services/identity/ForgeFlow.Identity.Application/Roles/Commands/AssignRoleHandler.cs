using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Application.Roles.Commands;

public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, AssignRoleResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<AssignRoleHandler> _logger;

    public AssignRoleHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<AssignRoleHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<AssignRoleResult> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new AssignRoleResult(false, "Kullanıcı bulunamadı.");
        }

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
        {
            return new AssignRoleResult(false, $"'{request.RoleName}' rolü bulunamadı.");
        }

        if (await _userManager.IsInRoleAsync(user, request.RoleName))
        {
            return new AssignRoleResult(false, $"Kullanıcı zaten '{request.RoleName}' rolüne sahip.");
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);

        if (result.Succeeded)
        {
            _logger.LogInformation("Assigned role {Role} to user {UserId}", request.RoleName, request.UserId);
            return new AssignRoleResult(true, null);
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return new AssignRoleResult(false, errors);
    }
}
