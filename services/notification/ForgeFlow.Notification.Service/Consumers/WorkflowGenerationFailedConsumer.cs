using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumer for WorkflowGenerationFailed events.
/// Notifies frontend clients via SignalR that workflow generation failed.
/// </summary>
public class WorkflowGenerationFailedConsumer : IConsumer<EventEnvelope<WorkflowGenerationFailed>>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<WorkflowGenerationFailedConsumer> _logger;

    public WorkflowGenerationFailedConsumer(IHubContext<ForgeHub> hubContext, ILogger<WorkflowGenerationFailedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<WorkflowGenerationFailed>> context)
    {
        var msg = context.Message;
        
        _logger.LogWarning(
            "Workflow generation failed for Project={ProjectId}, Error={Error}, notifying user {UserId}",
            msg.Data.ProjectId,
            msg.Data.ErrorMessage,
            msg.UserId);

        // SignalR message for workflow failure
        var notification = new NotificationMessage(
            Type: "WorkflowGenerationFailed",
            Title: "Workflow Oluşturma Hatası",
            Message: $"Workflow oluşturulurken bir hata oluştu: {msg.Data.ErrorMessage}",
            Data: new
            {
                msg.Data.ProjectId,
                msg.Data.ErrorCode,
                msg.Data.ErrorMessage
            }
        );

        try
        {
            // Send to user's personal group
            await _hubContext.Clients
                .Group($"user:{msg.UserId}")
                .SendAsync("Notification", notification, context.CancellationToken);

            // Also send to project group
            if (!string.IsNullOrEmpty(msg.Data.ProjectId) && 
                Guid.TryParse(msg.Data.ProjectId, out var projectGuid) && 
                projectGuid != Guid.Empty)
            {
                await _hubContext.Clients
                    .Group($"project:{projectGuid}")
                    .SendAsync("Notification", notification, context.CancellationToken);
            }

            // Publish a final progress message to stop the loading indicator
            var progressMessage = new AiProgressMessage(
                msg.EventId,
                $"Hata: {msg.Data.ErrorMessage}",
                100,
                true
            );

            await _hubContext.Clients.Group($"user:{msg.UserId}").SendAsync("AiProgress", progressMessage, context.CancellationToken);

            _logger.LogDebug("Sent workflow generation failed notification to user {UserId}", msg.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send workflow generation failed notification");
        }
    }
}
