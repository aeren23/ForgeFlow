using ForgeFlow.Identity.Application.Auth.Commands;
using ForgeFlow.Identity.Application.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Yeni kullanıcı kaydı
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(
            Email: request.Email,
            Password: request.Password,
            FullName: request.FullName
        ));

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new
        {
            userId = result.UserId,
            email = result.Email,
            message = "Kayıt başarılı"
        });
    }

    /// <summary>
    /// Kullanıcı girişi - Access token ve Refresh token döner
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(
            Email: request.Email,
            Password: request.Password
        ));

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt,
            userId = result.UserId,
            email = result.Email
        });
    }

    /// <summary>
    /// Refresh token ile yeni access token alma
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(
            RefreshToken: request.RefreshToken
        ));

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt
        });
    }

    /// <summary>
    /// Mevcut kullanıcının profil bilgilerini getirir
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // JWT'den UserId'yi al (sub claim)
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _mediator.Send(new GetProfileQuery(UserId: userId));
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "User not found" });
        }
    }
}

// Request DTOs
public record RegisterRequest(string Email, string Password, string? FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
