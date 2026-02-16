using ForgeFlow.AiOrchestrator.Application;
using ForgeFlow.AiOrchestrator.Infrastructure;
using ForgeFlow.AiOrchestrator.Infrastructure.Persistence;
using ForgeFlow.AiOrchestrator.Worker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog with Seq
builder.Services.AddSerilog((sp, lc) => lc
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("MassTransit", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ForgeFlow.AiOrchestrator")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://seq:5341"));

// Add Application layer (MediatR + Behaviors)
builder.Services.AddAiOrchestratorApplication();

// Add Infrastructure layer (AI Services, Repositories, DbContext)
builder.Services.AddAiOrchestratorInfrastructure(builder.Configuration);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<AiPlanRequestedConsumer>();
    x.AddConsumer<CodeReviewRequestedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
        var username = builder.Configuration["RabbitMq:Username"] ?? "forgeflow";
        var password = builder.Configuration["RabbitMq:Password"] ?? "forgeflow";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Configure the consumer endpoint
        cfg.ReceiveEndpoint("q.ai.plan.requested", e =>
        {
            // Retry policy for transient errors
            e.UseMessageRetry(r => r
                .Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

            // Configure the consumer
            e.ConfigureConsumer<AiPlanRequestedConsumer>(context);
        });

        // Code review consumer endpoint
        cfg.ReceiveEndpoint("q.ai.code-review", e =>
        {
            e.UseMessageRetry(r => r
                .Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<CodeReviewRequestedConsumer>(context);
        });
    });
});


var host = builder.Build();

// Auto-migrate database
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiOrchestratorDbContext>();
    // Ensure DB is created and migrated
    // Note: In production, consider separating this step
    await db.Database.MigrateAsync();
}

Log.Information("ForgeFlow AI Orchestrator starting...");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ForgeFlow AI Orchestrator terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
