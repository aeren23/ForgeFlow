namespace ForgeFlow.Contracts.Events;

public record ArtifactGenerated(
    string IssueId,
    string ProjectId,
    string ArtifactType,
    int Revision
);
