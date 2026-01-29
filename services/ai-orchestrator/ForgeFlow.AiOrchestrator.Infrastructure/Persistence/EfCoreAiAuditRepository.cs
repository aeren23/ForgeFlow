using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.AiOrchestrator.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IAiAuditRepository
/// </summary>
public class EfCoreAiAuditRepository : IAiAuditRepository
{
    private readonly AiOrchestratorDbContext _context;

    public EfCoreAiAuditRepository(AiOrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AiAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AiAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IEnumerable<AiAuditLog>> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return await _context.AiAuditLogs
            .Where(log => log.CorrelationId == correlationId)
            .OrderByDescending(log => log.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AiAuditLog>> GetByIssueKeyAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        return await _context.AiAuditLogs
            .Where(log => log.IssueKey == issueKey)
            .OrderByDescending(log => log.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a request has already been processed (for idempotency)
    /// </summary>
    public async Task<bool> IsRequestProcessedAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _context.AiAuditLogs
            .AnyAsync(log => log.RequestId == requestId && log.IsSuccess, cancellationToken);
    }

    /// <summary>
    /// Gets a successfully processed audit log by request ID (for returning cached response)
    /// </summary>
    public async Task<AiAuditLog?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _context.AiAuditLogs
            .Where(log => log.RequestId == requestId && log.IsSuccess)
            .OrderByDescending(log => log.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
