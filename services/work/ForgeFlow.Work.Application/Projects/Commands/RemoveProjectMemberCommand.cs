using MediatR;

namespace ForgeFlow.Work.Application.Projects.Commands;

public record RemoveProjectMemberCommand(string ProjectKey, string TargetUserId) : IRequest<bool>;
