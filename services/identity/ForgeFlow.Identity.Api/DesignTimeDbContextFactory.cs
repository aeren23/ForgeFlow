using ForgeFlow.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeFlow.Identity.Api;

/// <summary>
/// EF Core migration'lar için Design Time Factory.
/// Bu factory sadece development/migration sırasında kullanılır.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        // Migration için dummy connection string - gerçek deployment'ta kullanılmaz
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=ForgeFlow_Identity;User Id=sa;Password=YourStrong(!)Password123;TrustServerCertificate=True;");

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
