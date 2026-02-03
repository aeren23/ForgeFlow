using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

public record ApplyAiPlanCommand(
    string ProjectId,
    string ParentIssueKey,
    string PlanJson,
    string UserId,
    Guid RequestId
) : IRequest<bool>;
