using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumes AI processing progress events and pushes to connected clients.
/// </summary>
public class AiProgressConsumer : IConsumer<AiProcessingProgress>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<AiProgressConsumer> _logger;

    public AiProgressConsumer(IHubContext<ForgeHub> hubContext, ILogger<AiProgressConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiProcessingProgress> context)
    {
        var msg = context.Message;
        _logger.LogInformation("AI Progress: {Message} ({Progress}%) for user {UserId}", 
            msg.Message, msg.ProgressPercentage, msg.UserId);

        // Create the SignalR message
        var progressMessage = new AiProgressMessage(
            msg.RequestId,
            msg.Message,
            msg.ProgressPercentage,
            msg.ProgressPercentage >= 100
        );

        // Send to specific user
        await _hubContext.Clients
            .Group($"user:{msg.UserId}")
            .SendAsync("AiProgress", progressMessage);

        // Also send to project group if ProjectId is available
        if (msg.ProjectId != Guid.Empty)
        {
            await _hubContext.Clients
                .Group($"project:{msg.ProjectId}")
                .SendAsync("AiProgress", progressMessage);
        }
    }
}
