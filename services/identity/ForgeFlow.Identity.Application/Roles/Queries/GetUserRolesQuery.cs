using MediatR;

namespace ForgeFlow.Identity.Application.Roles.Queries;

/// <summary>
/// Kullanıcının rollerini getir
/// </summary>
public record GetUserRolesQuery(string UserId) : IRequest<GetUserRolesResult>;

public record GetUserRolesResult(
    bool Found,
    string? UserId,
    IEnumerable<string> Roles,
    string? Error
);
