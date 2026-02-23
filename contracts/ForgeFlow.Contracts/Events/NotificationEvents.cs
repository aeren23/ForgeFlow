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
    DateTime Timestamp,
    bool CreateBranch = true
);

/// <summary>
/// GitHub Service → Notification Service
/// Branch oluşturulduğunda kullanıcıya bildirim gönderilir.
/// </summary>
public record BranchCreated(
    Guid ProjectId,
    string IssueKey,
    string BranchName,
    string AssigneeId,
    DateTime Timestamp
);

/// <summary>
/// GitHub Webhook → Work Service
/// PR merge edildiğinde issue durumu Done olarak güncellenir.
/// </summary>
public record GitHubPullRequestMerged(
    long RepositoryId,
    int PullNumber,
    string IssueKey
);

/// <summary>
/// GitHub Webhook → Work Service
/// PR açıldığında issue durumu InReview olarak güncellenir.
/// </summary>
public record GitHubPullRequestOpened(
    long RepositoryId,
    int PullNumber,
    string IssueKey,
    string PrTitle,
    string PrUrl,
    string AuthorLogin,
    DateTime Timestamp
);

/// <summary>
/// GitHub Webhook → Work Service
/// PR merge olmadan kapatıldığında issue InProgress'e geri döner.
/// </summary>
public record GitHubPullRequestClosed(
    long RepositoryId,
    int PullNumber,
    string IssueKey,
    DateTime Timestamp
);

/// <summary>
/// GitHub Webhook → Work Service veya AI Orchestrator
/// Push event - commit aktivitesi takibi için.
/// </summary>
public record GitHubPushReceived(
    long RepositoryId,
    string Ref,
    string HeadCommitSha,
    string[] ModifiedFiles,
    DateTime Timestamp
);

/// <summary>
/// GitHub Webhook → Artifact Service
/// PR durumu değiştiğinde artifact metadata'sını günceller.
/// </summary>
public record PullRequestStatusChanged(
    string IssueKey,
    int PullNumber,
    string Status,        // "open", "merged", "closed"
    DateTime Timestamp
);

/// <summary>
/// Artifact Service → Notification Service
/// CODE_REVIEW metadata güncellendiğinde SignalR ile frontend'e bildirir.
/// </summary>
public record CodeReviewUpdated(
    string IssueKey,
    Guid ProjectId,
    int PullNumber,
    string PrStatus       // "open", "merged", "closed"
);

// ========== CI/CD Integration Events ==========

/// <summary>
/// GitHub Webhook → Work Service + Artifact Service
/// GitHub Actions workflow/check suite sonucunu taşır.
/// WebhookController tarafından check_suite veya workflow_run event'lerinde yayınlanır.
/// </summary>
public record CiCdStatusReceived(
    string IssueKey,
    long RepositoryId,
    string WorkflowName,      // "Build & Test", "Deploy Staging" vb.
    string Status,            // "queued", "in_progress", "completed"
    string? Conclusion,       // "success", "failure", "cancelled", "skipped" (status=completed ise)
    string BranchName,
    string CommitSha,
    string? HtmlUrl,          // GitHub Actions run URL
    long RunId,               // Workflow run ID (idempotency için)
    DateTime Timestamp
);

/// <summary>
/// Work Service → Notification Service
/// Issue'ya bağlanmış CI/CD durumu değiştiğinde SignalR ile frontend'e bildirir.
/// </summary>
public record CiCdStatusUpdated(
    string IssueKey,
    Guid ProjectId,
    string WorkflowName,
    string Status,            // "queued", "in_progress", "success", "failure", "cancelled"
    string? HtmlUrl,
    DateTime Timestamp
);
