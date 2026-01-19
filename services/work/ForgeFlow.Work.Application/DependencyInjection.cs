using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.Work.Application;

/// <summary>
/// Application layer dependency injection
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkApplication(this IServiceCollection services)
    {
        // MediatR handlers'ı bu assembly'den kaydet
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
