using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Consumer for WorkflowGenerationCompleted events.
/// Notifies frontend clients via SignalR that workflow generation is complete.
/// </summary>
public class WorkflowGenerationCompletedConsumer : IConsumer<EventEnvelope<WorkflowGenerationCompleted>>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<WorkflowGenerationCompletedConsumer> _logger;

    public WorkflowGenerationCompletedConsumer(IHubContext<ForgeHub> hubContext, ILogger<WorkflowGenerationCompletedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<WorkflowGenerationCompleted>> context)
    {
        var msg = context.Message;
        
        _logger.LogInformation(
            "Workflow generation completed for Project={ProjectId}, notifying user {UserId}",
            msg.Data.ProjectId,
            msg.UserId);

        // SignalR message for workflow completion
        var notification = new NotificationMessage(
            Type: "WorkflowGenerationCompleted",
            Title: "Workflow Oluşturuldu",
            Message: $"GitHub Actions workflow '{msg.Data.WorkflowFileName}' başarıyla oluşturuldu.",
            Data: new
            {
                msg.Data.ProjectId,
                msg.Data.WorkflowFileName,
                msg.Data.WorkflowYaml,
                msg.Data.DurationMs,
                msg.Data.PromptTokens,
                msg.Data.CompletionTokens,
                msg.Data.UsedProvider
            }
        );

        try
        {
            // Send to user's personal group
            await _hubContext.Clients
                .Group($"user:{msg.UserId}")
                .SendAsync("Notification", notification, context.CancellationToken);

            // Also send to project group if project ID is available
            if (!string.IsNullOrEmpty(msg.Data.ProjectId) && 
                Guid.TryParse(msg.Data.ProjectId, out var projectGuid) && 
                projectGuid != Guid.Empty)
            {
                await _hubContext.Clients
                    .Group($"project:{projectGuid}")
                    .SendAsync("Notification", notification, context.CancellationToken);
            }

            _logger.LogDebug("Sent workflow generation completed notification to user {UserId}", msg.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send workflow generation completed notification");
        }
    }
}
