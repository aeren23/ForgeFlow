namespace ForgeFlow.Contracts.Events;

/// <summary>
/// Event published during AI processing to report progress.
/// Consumed by Notification Service to push real-time updates.
/// </summary>
public record AiProcessingProgress(
    Guid RequestId,
    Guid ProjectId,
    string UserId,
    string Message,
    int ProgressPercentage,
    DateTime Timestamp
);

/// <summary>
/// Event published when an issue's status changes.
/// Consumed by Notification Service to notify project boards.
/// </summary>
public record IssueStatusChanged(
    string IssueKey,
    Guid ProjectId,
    int OldStatus,
    int NewStatus,
    string UpdatedByUserId,
    DateTime Timestamp
);

/// <summary>
/// Generic user notification event.
/// Can be published from any service to notify a specific user.
/// </summary>
public record UserNotification(
    string UserId,
    string Type,
    string Title,
    string Message,
    object? Data = null
);

/// <summary>
/// Event published when an issue is assigned to a user.
/// GitHub Service will consume this to create feature branch.
/// </summary>
public record IssueAssigned(
    string IssueKey,
    string IssueTitle,
    Guid ProjectId,
    string? OldAssigneeId,
    string? NewAssigneeId,
    string? RepositoryUrl,
    string DefaultBranch,
    DateTime Timestamp
);
