using System.Security.Cryptography;
using System.Text;
using ForgeFlow.Artifact.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;

namespace ForgeFlow.Artifact.Application.Artifacts.Commands;

public class UpsertArtifactRevisionHandler : IRequestHandler<UpsertArtifactRevisionCommand, int>
{
    private readonly IArtifactRepository _repository;
    private readonly ILogger<UpsertArtifactRevisionHandler> _logger;

    public UpsertArtifactRevisionHandler(IArtifactRepository repository, ILogger<UpsertArtifactRevisionHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> Handle(UpsertArtifactRevisionCommand request, CancellationToken cancellationToken)
    {
        // 1. Idempotency Check: Bu CorrelationId ile daha önce bir revision kaydedilmiş mi?
        var existingRevision = await _repository.RevisionExistsByCorrelationIdAsync(request.CorrelationId, cancellationToken);

        if (existingRevision)
        {
            _logger.LogWarning(
                "Duplicate event detected. CorrelationId: {CorrelationId} already processed. Skipping.",
                request.CorrelationId);
            return -1; // Duplicate - skip processing
        }

        // 2. Eğer yoksa normal sürece devam et (Artifact bul/yarat, Revision ekle)
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

        // Add revision using the domain method (with correlationId for idempotency)
        var revision = artifact.AddRevision(request.ContentJson, contentHash, request.CorrelationId);

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
