using ForgeFlow.Contracts.Events;
using ForgeFlow.Notification.Service.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ForgeFlow.Notification.Service.Consumers;

public class GitHubInstallationCreatedConsumer : IConsumer<GitHubInstallationCreated>
{
    private readonly IHubContext<ForgeHub> _hubContext;
    private readonly ILogger<GitHubInstallationCreatedConsumer> _logger;

    public GitHubInstallationCreatedConsumer(
        IHubContext<ForgeHub> hubContext,
        ILogger<GitHubInstallationCreatedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GitHubInstallationCreated> context)
    {
        var message = context.Message;
        _logger.LogInformation("Broadcasting installation update for ID: {InstallationId}", message.InstallationId);

        // Broadcast to all clients to refresh their installation list
        await _hubContext.Clients.All.SendAsync("InstallationListUpdated", new
        {
            message.InstallationId,
            message.AccountLogin
        });
    }
}
