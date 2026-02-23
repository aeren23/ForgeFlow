using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

/// <summary>
/// CI/CD status değiştiğinde ilgili proje board'unu SignalR ile günceller.
/// Work Service → CiCdStatusUpdated event → bu consumer → SignalR broadcast
/// </summary>
public class CiCdStatusUpdatedConsumer : IConsumer<CiCdStatusUpdated>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<CiCdStatusUpdatedConsumer> _logger;

    public CiCdStatusUpdatedConsumer(IHubContext<ForgeHub> hubContext, ILogger<CiCdStatusUpdatedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CiCdStatusUpdated> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Broadcasting CI/CD update: Issue={IssueKey}, Workflow={Workflow}, Status={Status}",
            msg.IssueKey, msg.WorkflowName, msg.Status);

        // Extract project key from issue key (e.g., "PROJ-123" -> "PROJ")
        var projectKey = msg.IssueKey.Split('-')[0];

        var cicdUpdate = new
        {
            IssueKey = msg.IssueKey,
            ProjectId = msg.ProjectId,
            WorkflowName = msg.WorkflowName,
            Status = msg.Status,
            HtmlUrl = msg.HtmlUrl,
            Timestamp = msg.Timestamp
        };

        // İlgili proje board'undaki tüm kullanıcılara bildir
        await _hubContext.Clients
            .Group($"project:{projectKey}")
            .SendAsync("CiCdUpdate", cicdUpdate);

        _logger.LogInformation("CI/CD update broadcasted to project:{ProjectKey}", projectKey);
    }
}
