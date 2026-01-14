using ForgeFlow.Artifact.Application.Abstractions;
using MediatR;

namespace ForgeFlow.Artifact.Application.Artifacts.Queries;

public class GetLatestArtifactHandler : IRequestHandler<GetLatestArtifactQuery, LatestArtifactDto?>
{
    private readonly IArtifactRepository _repo;

    public GetLatestArtifactHandler(IArtifactRepository repo) => _repo = repo;

    public async Task<LatestArtifactDto?> Handle(GetLatestArtifactQuery request, CancellationToken ct)
    {
        var artifact = await _repo.FindAsync(request.ProjectId, request.IssueId, request.Type, ct);
        if (artifact is null) return null;

        // Senin entity'lerde revisions listesi var diye varsayıyorum.
        // Yoksa repo tarafında Include ile zaten yüklüyor olmalısın.
        var latest = artifact.Revisions
            .OrderByDescending(r => r.RevisionNo)
            .FirstOrDefault();

        if (latest is null) return null;

        return new LatestArtifactDto(
            ArtifactId: artifact.Id,
            ProjectId: artifact.ProjectId,
            IssueId: artifact.IssueId,
            Type: artifact.Type,
            RevisionNo: latest.RevisionNo,
            ContentJson: latest.ContentJson,
            ContentHash: latest.ContentHash,
            CreatedAtUtc: latest.CreatedAtUtc
        );
    }
}
