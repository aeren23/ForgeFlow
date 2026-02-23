using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Issues.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Work.Api.Consumers;

/// <summary>
/// GitHub Actions CI/CD durumu değiştiğinde issue'yu günceller.
/// GitHub Webhook → WebhookController → CiCdStatusReceived event → bu consumer
/// </summary>
public class CiCdStatusReceivedConsumer : IConsumer<CiCdStatusReceived>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CiCdStatusReceivedConsumer> _logger;

    public CiCdStatusReceivedConsumer(IMediator mediator, ILogger<CiCdStatusReceivedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CiCdStatusReceived> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "CI/CD Status Received: Issue={IssueKey}, Workflow={Workflow}, Status={Status}, Conclusion={Conclusion}",
            msg.IssueKey, msg.WorkflowName, msg.Status, msg.Conclusion ?? "N/A");

        // Status mapping: completed + conclusion → final status
        var status = msg.Status == "completed"
            ? (msg.Conclusion ?? "unknown")
            : msg.Status;

        try
        {
            var result = await _mediator.Send(new UpdateCiCdStatusCommand(
                IssueKey: msg.IssueKey,
                WorkflowName: msg.WorkflowName,
                Status: status,
                HtmlUrl: msg.HtmlUrl,
                CommitSha: msg.CommitSha,
                RunId: msg.RunId
            ), context.CancellationToken);

            _logger.LogInformation(
                "Issue {IssueKey} CI/CD status updated to '{Status}' (Workflow: {Workflow})",
                result.IssueKey, result.Status, msg.WorkflowName);
        }
        catch (InvalidOperationException ex)
        {
            // Issue key bulunamadı — branch adı ForgeFlow ile eşleşmiyor olabilir
            _logger.LogWarning(ex, "Issue {IssueKey} not found for CI/CD status update", msg.IssueKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update CI/CD status for issue {IssueKey}", msg.IssueKey);
            throw;
        }
    }
}
