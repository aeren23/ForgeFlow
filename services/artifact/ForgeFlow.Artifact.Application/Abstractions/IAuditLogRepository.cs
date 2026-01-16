using ForgeFlow.Artifact.Domain.Entities;

namespace ForgeFlow.Artifact.Application.Abstractions;

/// <summary>
/// Audit log repository interface - Infrastructure tarafından implement edilir.
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
