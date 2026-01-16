using ForgeFlow.Artifact.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;
using ArtifactRevisionEntity = ForgeFlow.Artifact.Domain.Entities.ArtifactRevision;

namespace ForgeFlow.Artifact.Infrastructure.Persistence;

public class ArtifactRepository : IArtifactRepository
{
    private readonly ArtifactDbContext _db;

    public ArtifactRepository(ArtifactDbContext db) => _db = db;

    public Task<ArtifactEntity?> FindAsync(string projectId, string issueId, string type, CancellationToken ct)
        => _db.Artifacts
            .Include(a => a.Revisions)
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.IssueId == issueId && a.Type == type, ct);

    public Task AddAsync(ArtifactEntity artifact, CancellationToken ct)
        => _db.Artifacts.AddAsync(artifact, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);

    public Task<bool> RevisionExistsByCorrelationIdAsync(string correlationId, CancellationToken ct)
        => _db.ArtifactRevisions.AnyAsync(r => r.CorrelationId == correlationId, ct);
}

