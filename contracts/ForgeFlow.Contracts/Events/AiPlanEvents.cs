namespace ForgeFlow.Contracts.Events;

/// <summary>
/// Event published when AI successfully generates a plan
/// </summary>
public record AiPlanGenerated(
    string IssueId,
    string ProjectId,
    string ArtifactType,
    string GeneratedContent,
    string UsedProvider,
    int PromptTokens,
    int CompletionTokens,
    long DurationMs
);

/// <summary>
/// Event published when AI plan generation fails
/// </summary>
public record AiPlanFailed(
    string IssueId,
    string ProjectId,
    string ErrorCode,
    string ErrorMessage,
    string UsedProvider
);
