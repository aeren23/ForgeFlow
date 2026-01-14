using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeFlow.Artifact.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// This is only used by the dotnet ef CLI tool.
/// </summary>
public class ArtifactDbContextFactory : IDesignTimeDbContextFactory<ArtifactDbContext>
{
    public ArtifactDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ArtifactDbContext>();
        
        // Use a dummy connection string for migrations
        // The actual connection string is provided at runtime via DI
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=ForgeFlow_Artifact;User Id=sa;Password=YourStrong(!)Password123;TrustServerCertificate=True;Encrypt=False");

        return new ArtifactDbContext(optionsBuilder.Options);
    }
}
