using MediatR;

namespace ForgeFlow.Artifact.Application.Artifacts.Commands;

public record UpsertArtifactRevisionCommand(
    string ProjectId,
    string IssueId,
    string Type,
    string ContentJson,
    string CorrelationId
) : IRequest<int>;
