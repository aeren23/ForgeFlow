using ForgeFlow.Identity.Application.Roles.Commands;
using ForgeFlow.Identity.Application.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Identity.Api.Controllers;

/// <summary>
/// Rol yönetimi controller'ı - Strict Clean Architecture
/// Sadece MediatR kullanır, doğrudan Identity bağımlılığı yok
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")] // Sadece Admin rolüne sahip kullanıcılar erişebilir
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IMediator mediator, ILogger<RolesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Tüm rolleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _mediator.Send(new GetRolesQuery());
        return Ok(result.Roles);
    }

    /// <summary>
    /// Kullanıcıya rol ata
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var result = await _mediator.Send(new AssignRoleCommand(
            UserId: request.UserId,
            RoleName: request.RoleName
        ));

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = $"'{request.RoleName}' rolü başarıyla atandı." });
    }

    /// <summary>
    /// Kullanıcıdan rol kaldır
    /// </summary>
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
    {
        var result = await _mediator.Send(new RemoveRoleCommand(
            UserId: request.UserId,
            RoleName: request.RoleName
        ));

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = $"'{request.RoleName}' rolü başarıyla kaldırıldı." });
    }

    /// <summary>
    /// Kullanıcının rollerini getir
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var result = await _mediator.Send(new GetUserRolesQuery(userId));

        if (!result.Found)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(new { userId = result.UserId, roles = result.Roles });
    }
}

public record AssignRoleRequest(string UserId, string RoleName);
