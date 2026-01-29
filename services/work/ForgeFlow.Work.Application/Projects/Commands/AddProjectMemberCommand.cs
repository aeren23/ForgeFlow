using MediatR;

namespace ForgeFlow.Work.Application.Projects.Commands;

public record AddProjectMemberCommand(
    string ProjectKey,
    string UserId,
    string Role
) : IRequest<bool>;
