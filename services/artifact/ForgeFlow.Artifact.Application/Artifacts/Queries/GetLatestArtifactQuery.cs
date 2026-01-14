using MediatR;

namespace ForgeFlow.Artifact.Application.Artifacts.Queries;

public record GetLatestArtifactQuery(string ProjectId, string IssueId, string Type)
    : IRequest<LatestArtifactDto?>;

public record LatestArtifactDto(
    Guid ArtifactId,
    string ProjectId,
    string IssueId,
    string Type,
    int RevisionNo,
    string ContentJson,
    string ContentHash,
    DateTime CreatedAtUtc
);
