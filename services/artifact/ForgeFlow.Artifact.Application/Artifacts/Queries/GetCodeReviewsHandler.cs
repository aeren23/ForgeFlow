using ForgeFlow.Artifact.Application.Abstractions;
using MediatR;

namespace ForgeFlow.Artifact.Application.Artifacts.Queries;

public class GetCodeReviewsHandler : IRequestHandler<GetCodeReviewsQuery, List<CodeReviewDto>>
{
    private readonly IArtifactRepository _repo;

    public GetCodeReviewsHandler(IArtifactRepository repo) => _repo = repo;

    public async Task<List<CodeReviewDto>> Handle(GetCodeReviewsQuery request, CancellationToken ct)
    {
        var artifact = await _repo.FindAsync(request.ProjectId, request.IssueId, "CODE_REVIEW", ct);
        if (artifact is null) return new List<CodeReviewDto>();

        return artifact.Revisions
            .OrderByDescending(r => r.RevisionNo)
            .Select(r => new CodeReviewDto(
                ArtifactId: artifact.Id,
                RevisionNo: r.RevisionNo,
                ContentJson: r.ContentJson,
                CorrelationId: r.CorrelationId,
                Metadata: r.Metadata,
                CreatedAtUtc: r.CreatedAtUtc
            ))
            .ToList();
    }
}
