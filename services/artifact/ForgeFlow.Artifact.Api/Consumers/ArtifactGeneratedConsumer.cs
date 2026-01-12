using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Artifact.Api.Consumers;

public class ArtifactGeneratedConsumer(ILogger<ArtifactGeneratedConsumer> logger) : IConsumer<EventEnvelope<ArtifactGenerated>>
{
    private readonly ILogger<ArtifactGeneratedConsumer> _logger = logger;


    public Task Consume(ConsumeContext<EventEnvelope<ArtifactGenerated>> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Artifact Service consumed ArtifactGenerated | CorrelationId={CorrelationId} IssueId={IssueId} Rev={Rev}",
            msg.CorrelationId,
            msg.Data.IssueId,
            msg.Data.Revision
        );

        return Task.CompletedTask;
    }
}
