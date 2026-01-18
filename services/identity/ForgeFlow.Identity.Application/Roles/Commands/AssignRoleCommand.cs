using MediatR;

namespace ForgeFlow.Identity.Application.Roles.Commands;

/// <summary>
/// Kullanıcıya rol atama komutu
/// </summary>
public record AssignRoleCommand(
    string UserId,
    string RoleName
) : IRequest<AssignRoleResult>;

public record AssignRoleResult(
    bool Succeeded,
    string? Error
);
