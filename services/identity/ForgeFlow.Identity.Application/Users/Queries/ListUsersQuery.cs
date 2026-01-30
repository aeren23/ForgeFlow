using MediatR;

namespace ForgeFlow.Identity.Application.Users.Queries;

public record ListUsersQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<ListUsersResult>;

public record ListUsersResult(
    List<UserDto> Items,
    int TotalCount
);

public record UserDto(
    string Id,
    string UserName,
    string Email,
    string? FullName
);
