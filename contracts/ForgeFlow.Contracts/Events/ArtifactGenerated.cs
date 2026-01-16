namespace ForgeFlow.Contracts.Events;

/// <summary>
/// Artifact üretildiğinde yayınlanan event.
/// </summary>
public record ArtifactGenerated(
    string IssueId,
    string ProjectId,
    string ArtifactType,
    int Revision,
    string UserId      // Kim üretti?
);

