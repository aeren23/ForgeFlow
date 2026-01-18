using ForgeFlow.Identity.Domain.Entities;
using MediatR;

namespace ForgeFlow.Identity.Application.Roles.Queries;

/// <summary>
/// Tüm rolleri listele
/// </summary>
public record GetRolesQuery : IRequest<GetRolesResult>;

public record GetRolesResult(IEnumerable<RoleDto> Roles);

public record RoleDto(
    string Id,
    string Name,
    string? Description,
    bool IsSystem,
    DateTime CreatedAtUtc
);
