using ForgeFlow.Work.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForgeFlow.Work.Infrastructure.Persistence.Configurations;

/// <summary>
/// Issue entity EF Core konfigürasyonu
/// </summary>
public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issues");

        builder.HasKey(i => i.Id);

        // Key - unique index (PRJ-123 format)
        builder.Property(i => i.Key)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(i => i.Key).IsUnique();

        // Title
        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(500);

        // Description - markdown, uzun metin
        builder.Property(i => i.Description)
            .HasMaxLength(10000);

        // ReporterId
        builder.Property(i => i.ReporterId)
            .IsRequired()
            .HasMaxLength(36);

        // AssigneeId
        builder.Property(i => i.AssigneeId)
            .HasMaxLength(36);

        // Timestamps
        builder.Property(i => i.CreatedAtUtc)
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc)
            .IsRequired();

        // Concurrency token
        builder.Property(i => i.RowVersion)
            .IsRowVersion();

        // Self-referential relationship for parent-child hierarchy
        builder.HasOne(i => i.ParentIssue)
            .WithMany(i => i.ChildIssues)
            .HasForeignKey(i => i.ParentIssueId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete for hierarchy

        // Indexes for common queries
        builder.HasIndex(i => i.ProjectId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.AssigneeId);
        builder.HasIndex(i => i.ReporterId);
        builder.HasIndex(i => new { i.ProjectId, i.Status });

        // GitHub Integration
        builder.Property(i => i.BranchName)
            .HasMaxLength(200);
        builder.HasIndex(i => i.BranchName);
    }
}
