using ForgeFlow.AiOrchestrator.Domain.Entities;

namespace ForgeFlow.AiOrchestrator.Domain.Abstractions;

/// <summary>
/// Repository interface for AI audit logs
/// </summary>
public interface IAiAuditRepository
{
    /// <summary>
    /// Adds a new audit log entry
    /// </summary>
    Task AddAsync(AiAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by correlation ID
    /// </summary>
    Task<IEnumerable<AiAuditLog>> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by issue key
    /// </summary>
    Task<IEnumerable<AiAuditLog>> GetByIssueKeyAsync(string issueKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a request has already been processed (for idempotency)
    /// </summary>
    Task<bool> IsRequestProcessedAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a successfully processed audit log by request ID (for returning cached response)
    /// </summary>
    Task<AiAuditLog?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
