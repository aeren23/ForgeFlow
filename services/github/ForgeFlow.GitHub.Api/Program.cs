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
builder.Services.AddScoped<IPullRequestService, PullRequestService>();

// HttpClient for inter-service communication (Artifact Service)
builder.Services.AddHttpClient("ArtifactService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ArtifactApiUrl"] ?? "http://localhost:5290");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<IssueAssignedConsumer>();
    x.AddConsumer<CodeReviewRequestedConsumer>();
    x.AddConsumer<CodeReviewCompletedConsumer>();

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

        // Code review: fetch diff + send to AI
        cfg.ReceiveEndpoint("q.github.code-review-requested", e =>
        {
            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<CodeReviewRequestedConsumer>(context);
        });

        // Code review: write review to GitHub PR
        cfg.ReceiveEndpoint("q.github.code-review-completed", e =>
        {
            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<CodeReviewCompletedConsumer>(context);
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
