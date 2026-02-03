using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumer for AiPlanGenerated events.
/// Notifies users when AI plan has been generated and issues are being created.
/// </summary>
public class AiPlanCompletedConsumer : IConsumer<EventEnvelope<AiPlanGenerated>>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<AiPlanCompletedConsumer> _logger;

    public AiPlanCompletedConsumer(IHubContext<ForgeHub> hubContext, ILogger<AiPlanCompletedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<AiPlanGenerated>> context)
    {
        var msg = context.Message;
        
        _logger.LogInformation(
            "AI Plan completed for Issue={IssueId}, notifying user {UserId}",
            msg.Data.IssueId,
            msg.UserId);

        // Send progress update - AI plan is ready, Work Service is now creating issues
        var progressMessage = new AiProgressMessage(
            Guid.NewGuid(),
            "AI plan alındı, issue'lar oluşturuluyor...",
            95,
            false
        );

        try
        {
            // Send to user's personal group
            await _hubContext.Clients
                .Group($"user:{msg.UserId}")
                .SendAsync("AiProgress", progressMessage, context.CancellationToken);

            // Also send to project group if project ID is available
            if (!string.IsNullOrEmpty(msg.Data.ProjectId) && 
                Guid.TryParse(msg.Data.ProjectId, out var projectGuid) && 
                projectGuid != Guid.Empty)
            {
                await _hubContext.Clients
                    .Group($"project:{projectGuid}")
                    .SendAsync("AiProgress", progressMessage, context.CancellationToken);
            }

            _logger.LogDebug("Sent AI plan completed notification to user {UserId}", msg.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send AI plan completed notification");
        }
    }
}
