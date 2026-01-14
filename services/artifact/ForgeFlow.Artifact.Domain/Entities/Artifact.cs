namespace ForgeFlow.Artifact.Domain.Entities;

public class Artifact
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string ProjectId { get; private set; } = default!;
    public string IssueId { get; private set; } = default!;
    public string Type { get; private set; } = default!; // BUNDLE_V1 vs.

    public string Status { get; private set; } = "Draft";

    public Guid? CurrentRevisionId { get; private set; }

    private readonly List<ArtifactRevision> _revisions = new();
    public IReadOnlyCollection<ArtifactRevision> Revisions => _revisions;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Artifact() { } // EF

    public Artifact(string projectId, string issueId, string type)
    {
        ProjectId = projectId;
        IssueId = issueId;
        Type = type;
    }

    public ArtifactRevision AddRevision(string contentJson, string contentHash)
    {
        var next = _revisions.Count == 0 ? 1 : _revisions.Max(r => r.RevisionNo) + 1;

        var rev = new ArtifactRevision(Id, next, contentJson, contentHash);
        _revisions.Add(rev);

        CurrentRevisionId = rev.Id;
        UpdatedAtUtc = DateTime.UtcNow;

        return rev;
    }
}
