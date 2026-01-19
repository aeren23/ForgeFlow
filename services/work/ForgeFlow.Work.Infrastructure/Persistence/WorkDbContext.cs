using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Infrastructure.Persistence;

/// <summary>
/// Work Service veritabanı context'i
/// </summary>
public class WorkDbContext : DbContext, IWorkDbContext
{
    public WorkDbContext(DbContextOptions<WorkDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurations uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkDbContext).Assembly);
    }
}
