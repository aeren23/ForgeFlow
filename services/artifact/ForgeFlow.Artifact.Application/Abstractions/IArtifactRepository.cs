using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;

namespace ForgeFlow.Artifact.Application.Abstractions;

public interface IArtifactRepository
{
    Task<ArtifactEntity?> FindAsync(string projectId, string issueId, string type, CancellationToken ct);
    Task AddAsync(ArtifactEntity artifact, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
