using ForgeFlow.Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // We will add a policy check later or check inside actions
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(UserManager<ApplicationUser> userManager, ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<bool> IsAdminAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("IsAdminAsync: User is not authenticated.");
            return false;
        }

        var claims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
        _logger.LogInformation("IsAdminAsync: User authenticatd. Claims: {Claims}", claims);

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            // Try fallback to "sub" claim if available and NameIdentifier didn't work
            var sub = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub))
            {
                user = await _userManager.FindByIdAsync(sub);
                if (user != null)
                {
                    _logger.LogWarning("IsAdminAsync: User found via 'sub' claim fallback.");
                }
            }
        }

        if (user == null)
        {
            _logger.LogWarning("IsAdminAsync: User not found via UserManager.");
            return false;
        }

        if (!user.IsSystemAdmin)
        {
            _logger.LogWarning("IsAdminAsync: User found ({Email}) but IsSystemAdmin is false.", user.Email);
            return false;
        }

        return true;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!await IsAdminAsync()) return Forbid();

        var totalUsers = await _userManager.Users.CountAsync();
        var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);

        return Ok(new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            BannedUsers = totalUsers - activeUsers
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        if (!await IsAdminAsync()) return Forbid();

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(u => u.UserName!.ToLower().Contains(search) || u.Email!.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FullName,
                u.IsSystemAdmin,
                u.IsActive,
                u.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            Items = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPut("users/{id}/ban")]
    public async Task<IActionResult> ToggleBan(string id)
    {
        if (!await IsAdminAsync()) return Forbid();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Prevent banning self
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser!.Id == user.Id) return BadRequest("You cannot ban yourself.");

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return Ok(new { Message = user.IsActive ? "User unbanned." : "User banned.", IsActive = user.IsActive });
    }
}
