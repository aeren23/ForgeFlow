namespace ForgeFlow.Contracts.Events;

/// <summary>
/// Event to request AI plan generation for an issue.
/// Published by Work Service when user clicks "Generate AI Plan".
/// </summary>
public record AiPlanRequested(
    string IssueId,
    string ProjectId,
    string RequestedByUserId,
    string BundleType,
    string Strictness,
    string? PreferredProvider = null  // "Gemini", "Groq", etc. Null = use default
);
