using MediatR;

namespace ForgeFlow.Identity.Application.Auth.Commands;

/// <summary>
/// Kullanıcı kayıt komutu
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string? FullName
) : IRequest<RegisterResult>;

/// <summary>
/// Kayıt sonucu
/// </summary>
public record RegisterResult(
    bool Succeeded,
    string? UserId,
    string? Email,
    IEnumerable<string>? Errors
);
