using ForgeFlow.Identity.Application.Abstractions;
using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Application.Auth.Commands;

/// <summary>
/// Refresh token handler'ı - Mevcut refresh token ile yeni access token alma
/// </summary>
public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        ILogger<RefreshTokenHandler> logger)
    {
        _tokenService = tokenService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Refresh token doğrula
        var (user, oldToken) = await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user == null || oldToken == null)
        {
            _logger.LogWarning("Refresh token validation failed");
            return new RefreshTokenResult(false, null, null, null, "Geçersiz veya süresi dolmuş refresh token.");
        }

        // Eski refresh token'ı iptal et
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        // Kullanıcının güncel rollerini al
        var roles = await _userManager.GetRolesAsync(user);

        // Yeni access token üret (roller dahil)
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        // Yeni refresh token üret
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user, cancellationToken);

        _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

        return new RefreshTokenResult(
            Succeeded: true,
            AccessToken: accessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresAt: expiresAt,
            Error: null
        );
    }
}

