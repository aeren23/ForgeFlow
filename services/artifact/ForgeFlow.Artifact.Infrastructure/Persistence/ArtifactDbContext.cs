using Microsoft.EntityFrameworkCore;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;
using ArtifactRevisionEntity = ForgeFlow.Artifact.Domain.Entities.ArtifactRevision;

namespace ForgeFlow.Artifact.Infrastructure.Persistence;

public class ArtifactDbContext : DbContext
{
    public ArtifactDbContext(DbContextOptions<ArtifactDbContext> options) : base(options) { }

    public DbSet<ArtifactEntity> Artifacts => Set<ArtifactEntity>();
    public DbSet<ArtifactRevisionEntity> ArtifactRevisions => Set<ArtifactRevisionEntity>();

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

            b.HasIndex(x => new { x.ArtifactId, x.RevisionNo }).IsUnique();
        });
    }
}
