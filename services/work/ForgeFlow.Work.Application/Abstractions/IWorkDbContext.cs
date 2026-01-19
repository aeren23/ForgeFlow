using ForgeFlow.Work.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Abstractions;

/// <summary>
/// Work DbContext abstraction - Application layer'ın Infrastructure'a bağımlı olmaması için
/// </summary>
public interface IWorkDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<Issue> Issues { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
