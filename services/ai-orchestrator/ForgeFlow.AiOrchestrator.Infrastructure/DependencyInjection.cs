using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Infrastructure.AiServices;
using ForgeFlow.AiOrchestrator.Infrastructure.ContextProviders;
using ForgeFlow.AiOrchestrator.Infrastructure.Options;
using ForgeFlow.AiOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.AiOrchestrator.Infrastructure;

/// <summary>
/// Dependency Injection extension methods for AI Orchestrator Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiOrchestratorInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<WorkServiceOptions>(configuration.GetSection(WorkServiceOptions.SectionName));

        // Register DbContext
        services.AddDbContext<AiOrchestratorDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AiOrchestratorDb")));

        // Register repositories
        services.AddScoped<IAiAuditRepository, EfCoreAiAuditRepository>();

        // Register AI services with HttpClient
        services.AddHttpClient<IAiService, GeminiAiService>("Gemini");
        services.AddHttpClient<IAiService, GroqAiService>("Groq");

        // Register all IAiService implementations for factory pattern
        services.AddScoped<GeminiAiService>();
        services.AddScoped<GroqAiService>();
        services.AddScoped<IEnumerable<IAiService>>(sp => new List<IAiService>
        {
            sp.GetRequiredService<GeminiAiService>(),
            sp.GetRequiredService<GroqAiService>()
        });

        // Register factory
        services.AddScoped<IAiServiceFactory, AiServiceFactory>();

        // Register context providers
        services.AddHttpClient<IContextProvider, HttpWorkContextProvider>("WorkService");
        services.AddScoped<HttpWorkContextProvider>();

        return services;
    }
}
