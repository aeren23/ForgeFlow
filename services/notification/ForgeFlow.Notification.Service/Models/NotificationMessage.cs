namespace ForgeFlow.Notification.Service.Models;

/// <summary>
/// Generic notification message to be sent via SignalR.
/// </summary>
public record NotificationMessage(
    string Type,
    string Title,
    string Message,
    object? Data = null
)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// AI Progress update for real-time streaming.
/// </summary>
public record AiProgressMessage(
    Guid RequestId,
    string Message,
    int ProgressPercentage,
    bool IsComplete = false
);

/// <summary>
/// Board update signal - tells frontend to refresh data.
/// </summary>
public record BoardUpdateMessage(
    string ProjectId,
    string IssueKey,
    string UpdateType, // "status_changed", "assigned", "created", "deleted"
    object? Data = null
);
