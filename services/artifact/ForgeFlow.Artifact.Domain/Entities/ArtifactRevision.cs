namespace ForgeFlow.Artifact.Domain.Entities;

public class ArtifactRevision
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ArtifactId { get; private set; }
    public Artifact Artifact { get; private set; } = null!; // Navigation property
    public int RevisionNo { get; private set; }

    public string ContentJson { get; private set; } = "{}";
    public string ContentHash { get; private set; } = default!;

    /// <summary>
    /// Ekstra bilgileri (OriginalType, Provider, TokenUsage vb.) JSON formatında saklar.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Idempotency için kullanılır. Aynı CorrelationId ile tekrar gelen event'ler işlenmez.
    /// </summary>
    public string? CorrelationId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private ArtifactRevision() { } // EF

    public ArtifactRevision(Guid artifactId, int revisionNo, string contentJson, string contentHash, string? correlationId = null, string? metadata = null)
    {
        ArtifactId = artifactId;
        RevisionNo = revisionNo;
        ContentJson = contentJson;
        ContentHash = contentHash;
        CorrelationId = correlationId;
        Metadata = metadata;
    }

    /// <summary>
    /// Metadata JSON'ını günceller (örn: PR durumu ekleme).
    /// </summary>
    public void UpdateMetadata(string metadata) => Metadata = metadata;
}

