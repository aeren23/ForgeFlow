using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Work.Api.Controllers;

[ApiController]
[Route("api/issues")]
public class IssuesController : ControllerBase
{
    private readonly IPublishEndpoint _publish;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(IPublishEndpoint publish, ILogger<IssuesController> logger) => (_publish, _logger) = (publish, logger);

    [HttpPost("{issueId}/generate")]
    public async Task<IActionResult> Generate(string issueId)
    {
        // Correlation: bir zinciri takip etmek için aynı id.
        var correlationId = Guid.NewGuid().ToString("N");

        var evt = new EventEnvelope<AiPlanRequested>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: correlationId,
            CausationId: null,
            Data: new AiPlanRequested(
                IssueId: issueId,
                ProjectId: "PRJ-1",
                RequestedByUserId: "USR-1",
                BundleType: "BUNDLE_V1",
                Strictness: "STANDARD"
            )
        );
        _logger.LogInformation("Published AiPlanRequested | CorrelationId={CorrelationId} IssueId={IssueId}", correlationId, issueId);

        await _publish.Publish(evt);

        return Ok(new { correlationId, issueId });
    }
}
