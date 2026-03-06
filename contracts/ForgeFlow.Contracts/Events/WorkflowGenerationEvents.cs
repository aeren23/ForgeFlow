namespace ForgeFlow.Contracts.Events;

/// <summary>
/// Kullanıcı "AI ile Workflow Oluştur" butonuna tıkladığında yayınlanır.
/// Work Service → AI Orchestrator (via MassTransit)
/// </summary>
public record WorkflowGenerationRequested(
    string ProjectId,
    string RequestedByUserId,
    string? PreferredProvider = null
);

/// <summary>
/// AI Orchestrator workflow YAML başarıyla ürettiğinde yayınlanır.
/// AI Orchestrator → Notification Service + Frontend
/// </summary>
public record WorkflowGenerationCompleted(
    string ProjectId,
    string WorkflowYaml,
    string WorkflowFileName,
    string UsedProvider,
    int PromptTokens,
    int CompletionTokens,
    long DurationMs
);

/// <summary>
/// Workflow üretimi başarısız olduğunda yayınlanır.
/// AI Orchestrator → Notification Service + Frontend
/// </summary>
public record WorkflowGenerationFailed(
    string ProjectId,
    string ErrorCode,
    string ErrorMessage
);
