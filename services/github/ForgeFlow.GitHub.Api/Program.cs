using ForgeFlow.GitHub.Application.Consumers;
using ForgeFlow.GitHub.Application.Services;
using ForgeFlow.GitHub.Infrastructure.GitHub;
using ForgeFlow.GitHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "GitHub")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// DbContext
builder.Services.AddDbContext<GitHubDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GitHubDb")));

// Services
builder.Services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IRepositoryContentService, RepositoryContentService>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<IssueAssignedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rabbitConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitConfig["Username"] ?? "guest");
            h.Password(rabbitConfig["Password"] ?? "guest");
        });

        // Explicitly subscribe to the expected queue
        cfg.ReceiveEndpoint("issue-assigned-github", e =>
        {
            e.ConfigureConsumer<IssueAssignedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ForgeFlow GitHub Service", Version = "v1" });
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GitHubDbContext>();
    try
    {
        db.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error applying migrations (database may not be ready yet)");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.MapControllers();

Log.Information("ForgeFlow GitHub Service starting...");
app.Run();
