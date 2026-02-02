using ForgeFlow.Identity.Application.Abstractions;
using MediatR;

namespace ForgeFlow.Identity.Application.Users.Queries;

public record BatchGetUsersQuery(List<string> UserIds) : IRequest<List<UserDto>>;


