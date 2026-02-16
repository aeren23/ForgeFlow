using ForgeFlow.AiOrchestrator.Application.Commands;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Worker.Consumers;

/// <summary>
/// CodeReviewWithDiffReady event'ini tüketir.
/// GitHub Service diff + plan ile zenginleştirip gönderir, bu consumer AI review üretir.
/// </summary>
public class CodeReviewRequestedConsumer : IConsumer<CodeReviewWithDiffReady>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CodeReviewRequestedConsumer> _logger;

    public CodeReviewRequestedConsumer(IMediator mediator, ILogger<CodeReviewRequestedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CodeReviewWithDiffReady> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "AI Orchestrator received CodeReviewWithDiffReady | Issue={IssueKey} PR=#{PullNumber}",
            msg.IssueKey, msg.PullNumber);

        try
        {
            var command = new GenerateCodeReviewCommand
            {
                RequestId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                ProjectId = msg.ProjectId,
                IssueKey = msg.IssueKey,
                PullNumber = msg.PullNumber,
                DiffContent = msg.DiffContent,
                OriginalPlanJson = msg.OriginalPlanJson,
                PrTitle = msg.PrTitle,
                PrDescription = msg.PrDescription,
                PreferredProvider = null // MVP: default provider (Gemini Flash 2.0)
            };

            var result = await _mediator.Send(command, context.CancellationToken);

            if (result.IsSuccess)
            {
                await context.Publish(new CodeReviewCompleted(
                    IssueKey: msg.IssueKey,
                    ProjectId: msg.ProjectId,
                    PullNumber: msg.PullNumber,
                    ReviewContent: result.ReviewContent ?? "{}",
                    UsedProvider: result.UsedProvider.ToString(),
                    PromptTokens: result.PromptTokens,
                    CompletionTokens: result.CompletionTokens,
                    DurationMs: result.DurationMs
                ));

                _logger.LogInformation(
                    "Code review completed | Issue={IssueKey} PR=#{PullNumber} Provider={Provider} Tokens={Tokens}",
                    msg.IssueKey, msg.PullNumber, result.UsedProvider,
                    result.PromptTokens + result.CompletionTokens);
            }
            else
            {
                await context.Publish(new CodeReviewFailed(
                    IssueKey: msg.IssueKey,
                    ProjectId: msg.ProjectId,
                    PullNumber: msg.PullNumber,
                    ErrorMessage: result.ErrorMessage ?? "Unknown error"
                ));

                _logger.LogWarning(
                    "Code review failed | Issue={IssueKey} PR=#{PullNumber}: {Error}",
                    msg.IssueKey, msg.PullNumber, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception in code review | Issue={IssueKey} PR=#{PullNumber}",
                msg.IssueKey, msg.PullNumber);
            throw; // MassTransit retry
        }
    }
}
