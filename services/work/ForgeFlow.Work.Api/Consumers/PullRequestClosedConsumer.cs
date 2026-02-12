using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Issues.Commands;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Work.Api.Consumers;

/// <summary>
/// PR merge olmadan kapatıldığında issue'yu InProgress'e geri döndürür.
/// GitHub Webhook → WebhookController → GitHubPullRequestClosed event → bu consumer
/// </summary>
public class PullRequestClosedConsumer : IConsumer<GitHubPullRequestClosed>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PullRequestClosedConsumer> _logger;

    public PullRequestClosedConsumer(IMediator mediator, ILogger<PullRequestClosedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GitHubPullRequestClosed> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "PR Closed (without merge): Moving issue {IssueKey} back to InProgress (PR #{PrNumber})",
            msg.IssueKey, msg.PullNumber);

        try
        {
            var result = await _mediator.Send(new ChangeIssueStatusCommand(
                Key: msg.IssueKey,
                NewStatus: IssueStatus.InProgress,
                IsSystemAction: true
            ), context.CancellationToken);

            _logger.LogInformation(
                "Issue {IssueKey} transitioned: {OldStatus} → {NewStatus} (PR #{PrNumber} closed without merge)",
                msg.IssueKey, result.OldStatus, result.NewStatus, msg.PullNumber);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Issue {IssueKey} not found for closed PR #{PrNumber}", msg.IssueKey, msg.PullNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition issue {IssueKey} back to InProgress", msg.IssueKey);
            throw;
        }
    }
}
