namespace ForgeFlow.Artifact.Domain.Entities;

public class ArtifactRevision
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ArtifactId { get; private set; }
    public int RevisionNo { get; private set; }

    public string ContentJson { get; private set; } = "{}";
    public string ContentHash { get; private set; } = default!;

    /// <summary>
    /// Idempotency için kullanılır. Aynı CorrelationId ile tekrar gelen event'ler işlenmez.
    /// </summary>
    public string? CorrelationId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private ArtifactRevision() { } // EF

    public ArtifactRevision(Guid artifactId, int revisionNo, string contentJson, string contentHash, string? correlationId = null)
    {
        ArtifactId = artifactId;
        RevisionNo = revisionNo;
        ContentJson = contentJson;
        ContentHash = contentHash;
        CorrelationId = correlationId;
    }
}

