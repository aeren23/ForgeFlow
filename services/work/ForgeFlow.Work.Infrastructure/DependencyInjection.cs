using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.Work.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database connection string
        var connectionString = configuration.GetConnectionString("WorkDb")
            ?? Environment.GetEnvironmentVariable("WorkDb")
            ?? throw new InvalidOperationException("WorkDb connection string not configured");

        // DbContext registration
        services.AddDbContext<WorkDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        // IWorkDbContext abstraction registration
        services.AddScoped<IWorkDbContext>(sp => sp.GetRequiredService<WorkDbContext>());

        return services;
    }
}
