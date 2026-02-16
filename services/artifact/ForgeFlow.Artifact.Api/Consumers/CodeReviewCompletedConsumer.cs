using ForgeFlow.Artifact.Application.Artifacts.Commands;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Artifact.Api.Consumers;

/// <summary>
/// CodeReviewCompleted event'ini tüketir.
/// AI review sonuçlarını Artifact olarak saklar (Type = "CODE_REVIEW").
/// Issue bazlı review geçmişi bu şekilde oluşur.
/// </summary>
public class CodeReviewCompletedConsumer : IConsumer<CodeReviewCompleted>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CodeReviewCompletedConsumer> _logger;

    public CodeReviewCompletedConsumer(IMediator mediator, ILogger<CodeReviewCompletedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CodeReviewCompleted> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Saving code review artifact | Issue={IssueKey} PR=#{PullNumber}",
            msg.IssueKey, msg.PullNumber);

        try
        {
            // Metadata: provider bilgisi ve token kullanımı
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                PullNumber = msg.PullNumber,
                Provider = msg.UsedProvider,
                PromptTokens = msg.PromptTokens,
                CompletionTokens = msg.CompletionTokens,
                DurationMs = msg.DurationMs
            });

            // UpsertArtifactRevisionCommand ile kaydet
            // Type = "CODE_REVIEW", her PR review yeni bir revision olur
            var command = new UpsertArtifactRevisionCommand(
                ProjectId: msg.ProjectId,
                IssueId: msg.IssueKey,
                Type: "CODE_REVIEW",
                ContentJson: msg.ReviewContent,
                CorrelationId: $"pr-{msg.PullNumber}",
                UserId: "system",
                Metadata: metadata
            );

            await _mediator.Send(command);

            _logger.LogInformation(
                "Code review artifact saved | Issue={IssueKey} PR=#{PullNumber}",
                msg.IssueKey, msg.PullNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save code review artifact | Issue={IssueKey} PR=#{PullNumber}",
                msg.IssueKey, msg.PullNumber);
            // Saklama hatası review akışını bozmamalı
        }
    }
}
