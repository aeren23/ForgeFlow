using ForgeFlow.GitHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.GitHub.Infrastructure.Persistence;

public class GitHubDbContext : DbContext
{
    public GitHubDbContext(DbContextOptions<GitHubDbContext> options) : base(options)
    {
    }

    public DbSet<GitHubInstallation> Installations => Set<GitHubInstallation>();
    public DbSet<RepositoryConnection> RepositoryConnections => Set<RepositoryConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GitHubInstallation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstallationId).IsUnique();
            entity.Property(e => e.AccountLogin).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccountType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.AccessToken).HasMaxLength(500);
        });

        modelBuilder.Entity<RepositoryConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId).IsUnique();
            entity.HasIndex(e => e.RepositoryId);
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DefaultBranch).HasMaxLength(100).HasDefaultValue("main");
            entity.Property(e => e.WebhookSecret).HasMaxLength(100);

            entity.HasOne(e => e.Installation)
                .WithMany(i => i.Repositories)
                .HasForeignKey(e => e.InstallationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
