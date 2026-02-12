using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Issues.Commands;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Work.Api.Consumers;

/// <summary>
/// PR açıldığında issue'yu otomatik olarak InReview'a geçirir.
/// GitHub Webhook → WebhookController → GitHubPullRequestOpened event → bu consumer
/// </summary>
public class PullRequestOpenedConsumer : IConsumer<GitHubPullRequestOpened>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PullRequestOpenedConsumer> _logger;

    public PullRequestOpenedConsumer(IMediator mediator, ILogger<PullRequestOpenedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GitHubPullRequestOpened> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "PR Opened: Moving issue {IssueKey} to InReview (PR #{PrNumber} by {Author})",
            msg.IssueKey, msg.PullNumber, msg.AuthorLogin);

        try
        {
            var result = await _mediator.Send(new ChangeIssueStatusCommand(
                Key: msg.IssueKey,
                NewStatus: IssueStatus.InReview,
                IsSystemAction: true
            ), context.CancellationToken);

            _logger.LogInformation(
                "Issue {IssueKey} transitioned: {OldStatus} → {NewStatus} (PR #{PrNumber})",
                msg.IssueKey, result.OldStatus, result.NewStatus, msg.PullNumber);
        }
        catch (InvalidOperationException ex)
        {
            // Issue key bulunamadı — branch adı ForgeFlow ile eşleşmiyor olabilir
            _logger.LogWarning(ex, "Issue {IssueKey} not found for PR #{PrNumber}", msg.IssueKey, msg.PullNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition issue {IssueKey} to InReview", msg.IssueKey);
            throw;
        }
    }
}
