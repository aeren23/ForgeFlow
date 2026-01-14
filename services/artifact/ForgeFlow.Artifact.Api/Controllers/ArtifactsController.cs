using ForgeFlow.Artifact.Application.Artifacts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Artifact.Api.Controllers;

[ApiController]
[Route("api/artifacts")]
public class ArtifactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ArtifactsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(
        [FromQuery] string issueId,
        [FromQuery] string type,
        [FromQuery] string projectId = "PRJ-1",
        CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetLatestArtifactQuery(projectId, issueId, type), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
