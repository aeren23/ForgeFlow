using ForgeFlow.Work.Api.Services;
using ForgeFlow.Work.Application;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Infrastructure;
using ForgeFlow.Work.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://seq"));

// MassTransit MUST be registered BEFORE MediatR handlers (they depend on IPublishEndpoint)
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // Consumers
    x.AddConsumer<ForgeFlow.Work.Api.Consumers.AiPlanGeneratedConsumer>();
    x.AddConsumer<ForgeFlow.Work.Api.Consumers.PullRequestOpenedConsumer>();
    x.AddConsumer<ForgeFlow.Work.Api.Consumers.PullRequestMergedConsumer>();
    x.AddConsumer<ForgeFlow.Work.Api.Consumers.PullRequestClosedConsumer>();
    x.AddConsumer<ForgeFlow.Work.Api.Consumers.CiCdStatusReceivedConsumer>();

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

        cfg.ConfigureEndpoints(context);
    });
});

// Add Work layers (MediatR handlers need IPublishEndpoint from MassTransit above)
builder.Services.AddWorkApplication();
builder.Services.AddWorkInfrastructure(builder.Configuration);

// Add services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkDbContext>();
    await db.Database.MigrateAsync();
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

