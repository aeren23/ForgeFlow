using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.Artifact.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddArtifactApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
