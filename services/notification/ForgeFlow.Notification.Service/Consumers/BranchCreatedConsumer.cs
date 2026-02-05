using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumes BranchCreated events and notifies users via SignalR.
/// </summary>
public class BranchCreatedConsumer : IConsumer<BranchCreated>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<BranchCreatedConsumer> _logger;

    public BranchCreatedConsumer(IHubContext<ForgeHub> hubContext, ILogger<BranchCreatedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BranchCreated> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Branch created for issue {IssueKey}: {BranchName}",
            msg.IssueKey, msg.BranchName);

        // Extract project key from issue key (e.g., "PROJ-123" -> "PROJ")
        var projectKey = msg.IssueKey.Split('-')[0];

        // Board update - issue'nun branch bilgisi güncellendi
        var boardUpdate = new BoardUpdateMessage(
            projectKey,
            msg.IssueKey,
            "branchCreated",
            new { BranchName = msg.BranchName }
        );

        // Notify all users viewing this project's board
        await _hubContext.Clients
            .Group($"project:{projectKey}")
            .SendAsync("BoardUpdate", boardUpdate);

        // Also notify the assignee directly
        if (!string.IsNullOrEmpty(msg.AssigneeId))
        {
            await _hubContext.Clients
                .User(msg.AssigneeId)
                .SendAsync("Notification", new
                {
                    type = "branch_created",
                    title = "Branch Oluşturuldu",
                    message = $"{msg.IssueKey} için '{msg.BranchName}' branch'i oluşturuldu.",
                    issueKey = msg.IssueKey,
                    branchName = msg.BranchName,
                    timestamp = msg.Timestamp
                });
        }
    }
}
