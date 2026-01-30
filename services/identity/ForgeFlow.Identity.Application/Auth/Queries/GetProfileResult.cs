namespace ForgeFlow.Identity.Application.Auth.Queries;

public record GetProfileResult(
    string Id,
    string Email,
    string? FullName,
    IList<string> Roles,
    DateTime CreatedAt
);
