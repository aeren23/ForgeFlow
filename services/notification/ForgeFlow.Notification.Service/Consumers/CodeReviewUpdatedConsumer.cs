using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using ForgeFlow.Notification.Service.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// Artifact Service -> Notification Service
/// Code Review metadata'sı güncellendiğinde (örn: PR statüsü değişti) frontend'e bildirir.
/// </summary>
public class CodeReviewUpdatedConsumer : IConsumer<CodeReviewUpdated>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<CodeReviewUpdatedConsumer> _logger;

    public CodeReviewUpdatedConsumer(IHubContext<ForgeHub> hubContext, ILogger<CodeReviewUpdatedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CodeReviewUpdated> context)
    {
        var msg = context.Message;

        _logger.LogInformation("Broadcasting CodeReviewUpdated for Issue={IssueKey} PR=#{PullNumber} Status={PrStatus}",
            msg.IssueKey, msg.PullNumber, msg.PrStatus);

        // Frontend'in beklediği format
        var reviewUpdate = new
        {
            issueKey = msg.IssueKey,
            pullNumber = msg.PullNumber,
            prStatus = msg.PrStatus.ToLowerInvariant() // "open", "merged", "closed"
        };

        // ProjectId boş ise broadcast yapamayız (grup projesine bağlı)
        if (msg.ProjectId != Guid.Empty)
        {
            // Proje grubuna gönder
            await _hubContext.Clients
                .Group($"project:{msg.ProjectId}")
                .SendAsync("ReviewUpdate", reviewUpdate);
        }
        else
        {
            _logger.LogWarning("ProjectId is empty for CodeReviewUpdated event (Issue={IssueKey}). Cannot broadcast to project group.", msg.IssueKey);
        }
    }
}
