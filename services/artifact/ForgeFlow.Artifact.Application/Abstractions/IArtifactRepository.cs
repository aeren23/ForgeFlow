using ForgeFlow.Artifact.Domain.Entities;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;

namespace ForgeFlow.Artifact.Application.Abstractions;

public interface IArtifactRepository
{
    Task<ArtifactEntity?> FindAsync(string projectId, string issueId, string type, CancellationToken ct);
    Task<ArtifactRevision?> FindRevisionByCorrelationIdAsync(string correlationId, string type, CancellationToken ct);
    void Add(ArtifactEntity artifact);
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Change Tracker'daki tüm entity'leri detach eder.
    /// Race condition retry senaryolarında kullanılır.
    /// </summary>
    void DetachAll();

    /// <summary>
    /// Idempotency kontrolü: Bu CorrelationId ile daha önce bir revision kaydedilmiş mi?
    /// </summary>
    Task<bool> RevisionExistsByCorrelationIdAsync(string correlationId, CancellationToken ct);
}

