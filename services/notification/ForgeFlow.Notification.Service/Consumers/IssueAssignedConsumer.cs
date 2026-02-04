using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumes issue assignment events and notifies relevant project board.
/// </summary>
public class IssueAssignedConsumer : IConsumer<IssueAssigned>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<IssueAssignedConsumer> _logger;

    public IssueAssignedConsumer(IHubContext<ForgeHub> hubContext, ILogger<IssueAssignedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IssueAssigned> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Issue {IssueKey} assigned: {OldAssignee} -> {NewAssignee}",
            msg.IssueKey, msg.OldAssigneeId, msg.NewAssigneeId);

        // Extract project key from issue key (e.g., "PROJ-123" -> "PROJ")
        var projectKey = msg.IssueKey.Split('-')[0];

        var updateMessage = new BoardUpdateMessage(
            projectKey,
            msg.IssueKey,
            "assigned",
            new { OldAssigneeId = msg.OldAssigneeId, NewAssigneeId = msg.NewAssigneeId }
        );

        // Notify all users viewing this project's board
        await _hubContext.Clients
            .Group($"project:{projectKey}")
            .SendAsync("BoardUpdate", updateMessage);
    }
}
