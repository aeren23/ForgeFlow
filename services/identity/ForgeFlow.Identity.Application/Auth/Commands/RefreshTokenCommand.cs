using MediatR;

namespace ForgeFlow.Identity.Application.Auth.Commands;

/// <summary>
/// Refresh token ile yeni access token alma komutu
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<RefreshTokenResult>;

/// <summary>
/// Refresh sonucu
/// </summary>
public record RefreshTokenResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string? Error
);
