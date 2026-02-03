using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Generic notification consumer for user-targeted notifications.
/// </summary>
public class NotificationConsumer : IConsumer<UserNotification>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(IHubContext<ForgeHub> hubContext, ILogger<NotificationConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserNotification> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Notification for user {UserId}: {Type} - {Title}", 
            msg.UserId, msg.Type, msg.Title);

        var notification = new NotificationMessage(
            msg.Type,
            msg.Title,
            msg.Message,
            msg.Data
        );

        // Send to specific user
        await _hubContext.Clients
            .Group($"user:{msg.UserId}")
            .SendAsync("Notification", notification);
    }
}
