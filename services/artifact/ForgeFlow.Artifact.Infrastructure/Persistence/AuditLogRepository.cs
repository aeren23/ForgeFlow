using ForgeFlow.Artifact.Application.Abstractions;
using ForgeFlow.Artifact.Domain.Entities;

namespace ForgeFlow.Artifact.Infrastructure.Persistence;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ArtifactDbContext _db;

    public AuditLogRepository(ArtifactDbContext db) => _db = db;

    public Task AddAsync(AuditLog auditLog, CancellationToken ct)
        => _db.AuditLogs.AddAsync(auditLog, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
