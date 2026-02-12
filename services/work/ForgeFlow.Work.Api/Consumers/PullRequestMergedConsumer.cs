using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Issues.Commands;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Work.Api.Consumers;

/// <summary>
/// PR merge edildiğinde issue'yu otomatik olarak Done'a geçirir.
/// GitHub Webhook → WebhookController → GitHubPullRequestMerged event → bu consumer
/// </summary>
public class PullRequestMergedConsumer : IConsumer<GitHubPullRequestMerged>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PullRequestMergedConsumer> _logger;

    public PullRequestMergedConsumer(IMediator mediator, ILogger<PullRequestMergedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GitHubPullRequestMerged> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "PR Merged: Moving issue {IssueKey} to Done (PR #{PrNumber})",
            msg.IssueKey, msg.PullNumber);

        try
        {
            var result = await _mediator.Send(new ChangeIssueStatusCommand(
                Key: msg.IssueKey,
                NewStatus: IssueStatus.Done,
                IsSystemAction: true
            ), context.CancellationToken);

            _logger.LogInformation(
                "Issue {IssueKey} transitioned: {OldStatus} → {NewStatus} (PR #{PrNumber} merged)",
                msg.IssueKey, result.OldStatus, result.NewStatus, msg.PullNumber);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Issue {IssueKey} not found for merged PR #{PrNumber}", msg.IssueKey, msg.PullNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition issue {IssueKey} to Done", msg.IssueKey);
            throw;
        }
    }
}
