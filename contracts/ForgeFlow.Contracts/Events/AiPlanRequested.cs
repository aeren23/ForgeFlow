namespace ForgeFlow.Contracts.Events;

public record AiPlanRequested(
    string IssueId,
    string ProjectId,
    string RequestedByUserId,
    string BundleType,
    string Strictness
);
