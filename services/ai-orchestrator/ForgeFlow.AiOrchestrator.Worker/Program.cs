using ForgeFlow.AiOrchestrator.Worker.Consumers;
using MassTransit;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((sp, lc) => lc
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://seq"));

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<AiPlanRequestedConsumer>();

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

        // Bu consumer hangi queue’dan dinleyecek?
        cfg.ReceiveEndpoint("q.ai.plan.requested", e =>
        {
            // Basit retry (geçici hatalarda)
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<AiPlanRequestedConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
