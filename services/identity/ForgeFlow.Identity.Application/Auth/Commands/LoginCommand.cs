using MediatR;

namespace ForgeFlow.Identity.Application.Auth.Commands;

/// <summary>
/// Kullanıcı giriş komutu
/// </summary>
public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

/// <summary>
/// Giriş sonucu
/// </summary>
public record LoginResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string? UserId,
    string? Email,
    string? Error,
    bool IsSystemAdmin = false
);
