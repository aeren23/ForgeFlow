using ForgeFlow.AiOrchestrator.Application.Behaviors;
using ForgeFlow.AiOrchestrator.Application.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.AiOrchestrator.Application;

/// <summary>
/// Dependency Injection extension methods for AI Orchestrator Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiOrchestratorApplication(this IServiceCollection services)
    {
        // Register MediatR and all handlers from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Register pipeline behaviors (order matters!)
            // Idempotency runs FIRST - short-circuits if already processed
            cfg.AddBehavior<IPipelineBehavior<GenerateAiPlanCommand, GenerateAiPlanResult>,
                IdempotencyBehavior<GenerateAiPlanCommand, GenerateAiPlanResult>>();

            // Audit logging runs SECOND - logs everything including short-circuited requests
            cfg.AddBehavior<IPipelineBehavior<GenerateAiPlanCommand, GenerateAiPlanResult>,
                AuditLoggingBehavior<GenerateAiPlanCommand, GenerateAiPlanResult>>();
        });

        return services;
    }
}
