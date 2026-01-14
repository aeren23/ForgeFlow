using ForgeFlow.Artifact.Application.Abstractions;
using ForgeFlow.Artifact.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.Artifact.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddArtifactInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ArtifactDbContext>(opt => opt.UseSqlServer(connectionString));
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        return services;
    }
}
