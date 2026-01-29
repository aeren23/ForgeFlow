using ForgeFlow.AiOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.AiOrchestrator.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for AI Orchestrator audit logs
/// </summary>
public class AiOrchestratorDbContext : DbContext
{
    public AiOrchestratorDbContext(DbContextOptions<AiOrchestratorDbContext> options) 
        : base(options)
    {
    }

    public DbSet<AiAuditLog> AiAuditLogs => Set<AiAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AiAuditLog>(entity =>
        {
            entity.ToTable("AiAuditLogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IssueKey)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ModelName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SystemPrompt)
                .IsRequired();

            entity.Property(e => e.UserPrompt)
                .IsRequired();

            entity.Property(e => e.ErrorCode)
                .HasMaxLength(50);

            entity.Property(e => e.UserId)
                .HasMaxLength(128);

            // Indexes for common queries
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.IssueKey);
            entity.HasIndex(e => e.CreatedAtUtc);
        });
    }
}
