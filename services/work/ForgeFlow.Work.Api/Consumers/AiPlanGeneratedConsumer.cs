using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Issues.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Work.Api.Consumers;

public class AiPlanGeneratedConsumer : IConsumer<EventEnvelope<AiPlanGenerated>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AiPlanGeneratedConsumer> _logger;

    public AiPlanGeneratedConsumer(IMediator mediator, ILogger<AiPlanGeneratedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<AiPlanGenerated>> context)
    {
        var msg = context.Message;

        _logger.LogInformation("Processing AI Plan for Epic {EpicId}", msg.Data.IssueId);

        var command = new ApplyAiPlanCommand(
            ProjectId: msg.Data.ProjectId,
            ParentIssueKey: msg.Data.IssueId,
            PlanJson: msg.Data.GeneratedContent,
            UserId: msg.UserId
        );

        var result = await _mediator.Send(command, context.CancellationToken);

        if (result)
        {
            _logger.LogInformation("AI Plan successfully applied for Epic {EpicId}", msg.Data.IssueId);
        }
        else
        {
            _logger.LogWarning("AI Plan application failed or no items created for Epic {EpicId}", msg.Data.IssueId);
        }
    }
}
