using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Api.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Work.Api.Controllers;

[ApiController]
[Route("api/issues")]
public class IssuesController : ControllerBase
{
    private readonly IPublishEndpoint _publish;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(
        IPublishEndpoint publish,
        ICurrentUserService currentUser,
        ILogger<IssuesController> logger)
    {
        _publish = publish;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("{issueId}/generate")]
    public async Task<IActionResult> Generate(string issueId)
    {
        // Correlation: bir zinciri takip etmek için aynı id.
        var correlationId = Guid.NewGuid().ToString("N");

        // Gateway X-User-Id header'ından gerçek kullanıcı ID'sini al
        var userId = _currentUser.UserId ?? "anonymous";

        var evt = new EventEnvelope<AiPlanRequested>(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: correlationId,
            UserId: userId,  // Gerçek kullanıcı ID'si (Gateway'den)
            CausationId: null,
            Data: new AiPlanRequested(
                IssueId: issueId,
                ProjectId: "PRJ-1",
                RequestedByUserId: userId,
                BundleType: "BUNDLE_V1",
                Strictness: "STANDARD"
            )
        );

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId
        }))
        {
            _logger.LogInformation(
                "Published AiPlanRequested | IssueId={IssueId}",
                issueId);
        }

        await _publish.Publish(evt);

        return Ok(new { correlationId, issueId, userId });
    }
}
