using MediatR;

namespace ForgeFlow.Work.Application.Projects.Commands;

public record UpdateProjectMemberRoleCommand(
    string ProjectKey,
    string UserId,
    string Role) : IRequest<bool>;
