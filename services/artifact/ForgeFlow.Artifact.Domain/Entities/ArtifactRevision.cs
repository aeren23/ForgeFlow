namespace ForgeFlow.Artifact.Domain.Entities;

public class ArtifactRevision
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ArtifactId { get; private set; }
    public int RevisionNo { get; private set; }

    public string ContentJson { get; private set; } = "{}";
    public string ContentHash { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private ArtifactRevision() { } // EF

    public ArtifactRevision(Guid artifactId, int revisionNo, string contentJson, string contentHash)
    {
        ArtifactId = artifactId;
        RevisionNo = revisionNo;
        ContentJson = contentJson;
        ContentHash = contentHash;
    }
}
