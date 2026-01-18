using ForgeFlow.Identity.Application.Abstractions;
using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Application.Auth.Commands;

/// <summary>
/// Kullanıcı giriş handler'ı - UserManager ile password doğrulama, TokenService ile JWT üretimi
/// </summary>
public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<LoginHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
            return new LoginResult(false, null, null, null, null, null, "Email veya şifre hatalı.");
        }

        // Password doğrulama - UserManager üzerinden
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("Login failed: Invalid password for {Email}", request.Email);
            return new LoginResult(false, null, null, null, null, null, "Email veya şifre hatalı.");
        }

        // Kullanıcının rollerini al
        var roles = await _userManager.GetRolesAsync(user);

        // Access token üret (roller dahil)
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        // Refresh token üret ve kaydet
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user, cancellationToken);

        _logger.LogInformation("User logged in successfully: {Email} ({UserId}) Roles: {Roles}",
            user.Email, user.Id, string.Join(", ", roles));

        return new LoginResult(
            Succeeded: true,
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAt: expiresAt,
            UserId: user.Id,
            Email: user.Email,
            Error: null
        );
    }
}
