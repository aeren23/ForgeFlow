using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Application.Roles.Commands;

public class RemoveRoleHandler : IRequestHandler<RemoveRoleCommand, RemoveRoleResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RemoveRoleHandler> _logger;

    public RemoveRoleHandler(UserManager<ApplicationUser> userManager, ILogger<RemoveRoleHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<RemoveRoleResult> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new RemoveRoleResult(false, "Kullanıcı bulunamadı.");
        }

        if (!await _userManager.IsInRoleAsync(user, request.RoleName))
        {
            return new RemoveRoleResult(false, $"Kullanıcı '{request.RoleName}' rolüne sahip değil.");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, request.RoleName);

        if (result.Succeeded)
        {
            _logger.LogInformation("Removed role {Role} from user {UserId}", request.RoleName, request.UserId);
            return new RemoveRoleResult(true, null);
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return new RemoveRoleResult(false, errors);
    }
}
