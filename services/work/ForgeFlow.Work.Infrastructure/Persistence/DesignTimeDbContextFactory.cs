using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeFlow.Work.Infrastructure.Persistence;

/// <summary>
/// EF Core migration'ları için design-time factory
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WorkDbContext>
{
    public WorkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkDbContext>();
        
        // Design-time için dummy connection string
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=ForgeFlow_Work;Trusted_Connection=True;TrustServerCertificate=True;");

        return new WorkDbContext(optionsBuilder.Options);
    }
}
