using ForgeFlow.Work.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForgeFlow.Work.Infrastructure.Persistence.Configurations;

/// <summary>
/// Project entity EF Core konfigürasyonu
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        // Key - unique index, 2-5 karakter
        builder.Property(p => p.Key)
            .IsRequired()
            .HasMaxLength(5);
        builder.HasIndex(p => p.Key).IsUnique();

        // Name
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Description
        builder.Property(p => p.Description)
            .HasMaxLength(4000);

        // Repository
        builder.Property(p => p.RepositoryUrl)
            .HasMaxLength(500);

        builder.Property(p => p.DefaultBranch)
            .HasMaxLength(100)
            .HasDefaultValue("main");

        // TechStack - JSON array olarak sakla
        builder.Property(p => p.TechStack)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .HasMaxLength(1000);

        // CreatorId
        builder.Property(p => p.CreatorId)
            .IsRequired()
            .HasMaxLength(36); // GUID string length

        // Timestamps
        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        // Issues relationship
        builder.HasMany(p => p.Issues)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
