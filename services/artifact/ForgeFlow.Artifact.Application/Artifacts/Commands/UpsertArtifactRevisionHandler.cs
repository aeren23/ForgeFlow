using System.Security.Cryptography;
using System.Text;
using ForgeFlow.Artifact.Application.Abstractions;
using MediatR;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;

namespace ForgeFlow.Artifact.Application.Artifacts.Commands;

public class UpsertArtifactRevisionHandler : IRequestHandler<UpsertArtifactRevisionCommand, int>
{
    private readonly IArtifactRepository _repository;

    public UpsertArtifactRevisionHandler(IArtifactRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(UpsertArtifactRevisionCommand request, CancellationToken cancellationToken)
    {
        // Find existing artifact or create new one
        var artifact = await _repository.FindAsync(
            request.ProjectId,
            request.IssueId,
            request.Type,
            cancellationToken);

        if (artifact == null)
        {
            // Create new artifact using the proper constructor
            artifact = new ArtifactEntity(
                request.ProjectId,
                request.IssueId,
                request.Type
            );

            await _repository.AddAsync(artifact, cancellationToken);
        }

        // Calculate content hash
        var contentHash = ComputeHash(request.ContentJson);

        // Add revision using the domain method
        var revision = artifact.AddRevision(request.ContentJson, contentHash);

        await _repository.SaveChangesAsync(cancellationToken);

        return revision.RevisionNo;
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
