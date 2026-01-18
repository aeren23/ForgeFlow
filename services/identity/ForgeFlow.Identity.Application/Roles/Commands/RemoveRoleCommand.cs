using MediatR;

namespace ForgeFlow.Identity.Application.Roles.Commands;

/// <summary>
/// Kullanıcıdan rol kaldırma komutu
/// </summary>
public record RemoveRoleCommand(
    string UserId,
    string RoleName
) : IRequest<RemoveRoleResult>;

public record RemoveRoleResult(
    bool Succeeded,
    string? Error
);
