using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumes issue status change events and notifies relevant users/projects.
/// </summary>
public class IssueChangedConsumer : IConsumer<IssueStatusChanged>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<IssueChangedConsumer> _logger;

    public IssueChangedConsumer(IHubContext<ForgeHub> hubContext, ILogger<IssueChangedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IssueStatusChanged> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Issue {IssueKey} status changed: {OldStatus} -> {NewStatus}", 
            msg.IssueKey, msg.OldStatus, msg.NewStatus);

        // Extract project key from issue key (e.g., "PROJ-123" -> "PROJ")
        var projectKey = msg.IssueKey.Split('-')[0];

        var updateMessage = new BoardUpdateMessage(
            projectKey,
            msg.IssueKey,
            "status_changed",
            new { OldStatus = msg.OldStatus, NewStatus = msg.NewStatus }
        );

        // Notify all users viewing this project's board
        await _hubContext.Clients
            .Group($"project:{projectKey}")
            .SendAsync("BoardUpdate", updateMessage);
    }
}
