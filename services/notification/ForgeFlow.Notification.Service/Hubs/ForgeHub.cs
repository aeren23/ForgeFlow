using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Hubs;

/// <summary>
/// Real-time notification hub for ForgeFlow.
/// Clients connect here to receive live updates.
/// </summary>
[Authorize]
public class ForgeHub : Hub
{
    private readonly ILogger<ForgeHub> _logger;

    public ForgeHub(ILogger<ForgeHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} connected to ForgeHub. ConnectionId: {ConnectionId}", 
            userId, Context.ConnectionId);

        // Join user-specific group for targeted messages
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from ForgeHub. ConnectionId: {ConnectionId}", 
            userId, Context.ConnectionId);

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a project-specific group to receive project updates.
    /// </summary>
    public async Task JoinProjectGroup(string projectId)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} joining project group {ProjectId}", userId, projectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    /// <summary>
    /// Leave a project-specific group.
    /// </summary>
    public async Task LeaveProjectGroup(string projectId)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} leaving project group {ProjectId}", userId, projectId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }
}
