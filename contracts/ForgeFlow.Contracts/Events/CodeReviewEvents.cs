namespace ForgeFlow.Contracts.Events;

/// <summary>
/// PR açıldığında WebhookController tarafından yayınlanır.
/// GitHub Service consumer → diff çeker → AI Orchestrator'a gönderir.
/// </summary>
public record CodeReviewRequested(
    string IssueKey,
    string ProjectId,
    int PullNumber,
    string PrTitle,
    string PrUrl,
    string RepositoryOwner,
    string RepositoryName,
    long InstallationId,
    DateTime Timestamp
);

/// <summary>
/// GitHub Service tarafından diff ile zenginleştirilmiş review talebi.
/// AI Orchestrator bu event'i tüketip review üretir.
/// </summary>
public record CodeReviewWithDiffReady(
    string IssueKey,
    string ProjectId,
    int PullNumber,
    string PrTitle,
    string PrDescription,
    string DiffContent,
    string? OriginalPlanJson,
    DateTime Timestamp
);

/// <summary>
/// AI Orchestrator code review tamamladığında yayınlanır.
/// GitHub Service → PR'a yorum yazar, Artifact Service → review'ı saklar.
/// </summary>
public record CodeReviewCompleted(
    string IssueKey,
    string ProjectId,
    int PullNumber,
    string ReviewContent,     // JSON: findings, score, summary
    string UsedProvider,
    int PromptTokens,
    int CompletionTokens,
    long DurationMs
);

/// <summary>
/// AI code review başarısız olduğunda yayınlanır.
/// </summary>
public record CodeReviewFailed(
    string IssueKey,
    string ProjectId,
    int PullNumber,
    string ErrorMessage
);
