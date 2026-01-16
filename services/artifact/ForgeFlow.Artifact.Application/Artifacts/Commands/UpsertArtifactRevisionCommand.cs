using ForgeFlow.Artifact.Application.Abstractions;
using MediatR;

namespace ForgeFlow.Artifact.Application.Artifacts.Commands;

/// <summary>
/// Artifact revision oluşturma/güncelleme komutu.
/// IAuditLoggable implementasyonu sayesinde otomatik audit loglanır.
/// </summary>
public record UpsertArtifactRevisionCommand(
    string ProjectId,
    string IssueId,
    string Type,
    string ContentJson,
    string CorrelationId,
    string UserId
) : IRequest<int>, IAuditLoggable
{
    // IAuditLoggable implementation
    public string EntityName => "ArtifactRevision";
    public string EntityId => $"{ProjectId}:{IssueId}:{Type}"; // Composite key
    public string Action => "Create";
}
