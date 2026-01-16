using ForgeFlow.Artifact.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.Artifact.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddArtifactApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // AuditBehavior'ı pipeline'a ekle - IAuditLoggable command'ları otomatik loglanır
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        });

        return services;
    }
}

