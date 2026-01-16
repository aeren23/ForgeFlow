using Microsoft.EntityFrameworkCore;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;
using ArtifactRevisionEntity = ForgeFlow.Artifact.Domain.Entities.ArtifactRevision;
using AuditLogEntity = ForgeFlow.Artifact.Domain.Entities.AuditLog;

namespace ForgeFlow.Artifact.Infrastructure.Persistence;

public class ArtifactDbContext : DbContext
{
    public ArtifactDbContext(DbContextOptions<ArtifactDbContext> options) : base(options) { }

    public DbSet<ArtifactEntity> Artifacts => Set<ArtifactEntity>();
    public DbSet<ArtifactRevisionEntity> ArtifactRevisions => Set<ArtifactRevisionEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArtifactEntity>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.ProjectId).IsRequired();
            b.Property(x => x.IssueId).IsRequired();
            b.Property(x => x.Type).IsRequired();
            b.Property(x => x.Status).IsRequired();

            b.HasIndex(x => new { x.ProjectId, x.IssueId, x.Type }).IsUnique();

            // Revisions navigation'ı backing field ile çalışsın:
            var nav = b.Metadata.FindNavigation(nameof(ArtifactEntity.Revisions));
            nav!.SetPropertyAccessMode(PropertyAccessMode.Field);

            b.HasMany(x => x.Revisions)
                .WithOne()
                .HasForeignKey(nameof(ArtifactRevisionEntity.ArtifactId))
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ArtifactRevisionEntity>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.ContentJson).IsRequired();
            b.Property(x => x.ContentHash).IsRequired();

            // Idempotency için CorrelationId unique index
            // Kod tarafında kontrolü kaçırsak bile DB hata fırlatır ve tutarlılık bozulmaz
            b.HasIndex(x => x.CorrelationId)
                .IsUnique()
                .HasFilter("[CorrelationId] IS NOT NULL"); // NULL değerler hariç

            b.HasIndex(x => new { x.ArtifactId, x.RevisionNo }).IsUnique();
        });

        // AuditLog mapping
        modelBuilder.Entity<AuditLogEntity>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
            b.Property(x => x.EntityId).IsRequired().HasMaxLength(200);
            b.Property(x => x.Action).IsRequired().HasMaxLength(50);
            b.Property(x => x.UserId).IsRequired().HasMaxLength(100);
            b.Property(x => x.CorrelationId).IsRequired().HasMaxLength(100);
            b.Property(x => x.Changes).HasColumnType("nvarchar(max)");

            // Sorgulama için indexler
            b.HasIndex(x => x.CorrelationId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => new { x.EntityName, x.EntityId });
        });
    }
}

