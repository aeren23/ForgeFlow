using ForgeFlow.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Identity.Infrastructure.Persistence;

/// <summary>
/// Identity DbContext - ASP.NET Core Identity tablolarını ve RefreshToken'ları yönetir.
/// ApplicationUser ve ApplicationRole custom entity'lerini kullanır.
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser custom alanları
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.FullName).HasMaxLength(256);
            b.Property(x => x.EmailVerifiedAt);
            b.Property(x => x.CreatedAtUtc);
        });

        // ApplicationRole custom alanları
        builder.Entity<ApplicationRole>(b =>
        {
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.CreatedAtUtc);
            b.Property(x => x.IsSystem);
        });

        // RefreshToken mapping
        builder.Entity<RefreshToken>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Token).IsRequired().HasMaxLength(500);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.ExpiresAt);
            b.Property(x => x.CreatedAt);
            b.Property(x => x.RevokedAt);

            // UserId indexi - hızlı sorgulama için
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Token).IsUnique();

            // Navigation property
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

