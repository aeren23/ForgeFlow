using MediatR;

namespace ForgeFlow.Identity.Application.Auth.Queries;

public record GetProfileQuery(string UserId) : IRequest<GetProfileResult>;
