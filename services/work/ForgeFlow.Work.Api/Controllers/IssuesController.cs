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

        // TODO: Gerçek uygulamada bu JWT'den veya HttpContext'ten alınacak
        var userId = "USR-1"; // Placeholder - Identity entegrasyonunda güncellenecek

        var evt = new EventEnvelope<AiPlanRequested>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: correlationId,
            UserId: userId,  // Audit trail için kullanıcı bilgisi
            CausationId: null,
            Data: new AiPlanRequested(
                IssueId: issueId,
                ProjectId: "PRJ-1",
                RequestedByUserId: userId,
                BundleType: "BUNDLE_V1",
                Strictness: "STANDARD"
            )
        );

        _logger.LogInformation(
            "Published AiPlanRequested | CorrelationId={CorrelationId} IssueId={IssueId} UserId={UserId}",
            correlationId, issueId, userId);

        await _publish.Publish(evt);

        return Ok(new { correlationId, issueId, userId });
    }
}

