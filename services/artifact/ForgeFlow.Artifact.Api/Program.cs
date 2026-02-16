using ForgeFlow.Artifact.Api.Consumers;
using ForgeFlow.Artifact.Application;
using ForgeFlow.Artifact.Infrastructure;
using ForgeFlow.Artifact.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://seq"));

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<ArtifactGeneratedConsumer>();
    x.AddConsumer<AiPlanGeneratedConsumer>();
    x.AddConsumer<CodeReviewCompletedConsumer>();
    x.AddConsumer<PullRequestStatusChangedConsumer>();

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

        cfg.ReceiveEndpoint("q.artifact.generated", e =>
        {
            // Geçici hatalar (DB timeout vb.) için 3 kez dene (artan aralıklarla)
            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<ArtifactGeneratedConsumer>(context);
        });

        cfg.ReceiveEndpoint("q.artifact.ai-plan-generated", e =>
        {
            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<AiPlanGeneratedConsumer>(context);
        });

        cfg.ReceiveEndpoint("q.artifact.code-review-completed", e =>
        {
            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<CodeReviewCompletedConsumer>(context);
        });

        cfg.ReceiveEndpoint("q.artifact.pr-status-changed", e =>
        {
            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<PullRequestStatusChangedConsumer>(context);
        });
    });
});

var cs = builder.Configuration.GetConnectionString("Db");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("ConnectionStrings:Db is missing for Artifact service.");

builder.Services.AddArtifactInfrastructure(cs);
builder.Services.AddArtifactApplication();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArtifactDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
