using System.Text;
using ForgeFlow.Notification.Service.Consumers;
using ForgeFlow.Notification.Service.Hubs;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// JWT Configuration
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ForgeFlow.Identity";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ForgeFlow.Services";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub" // Map sub claim to User.Identity.Name
        };

        // SignalR uses query string for token in WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR with Redis Backplane
var redisConnection = Environment.GetEnvironmentVariable("Redis__ConnectionString") ?? "localhost:6379";
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "ForgeFlow";
    });

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AiProgressConsumer>();
    x.AddConsumer<IssueChangedConsumer>();
    x.AddConsumer<NotificationConsumer>();
    x.AddConsumer<AiPlanCompletedConsumer>();
    x.AddConsumer<GitHubInstallationCreatedConsumer>();
    x.AddConsumer<IssueAssignedConsumer>();
    x.AddConsumer<IssueAssignedConsumer>();
    x.AddConsumer<BranchCreatedConsumer>();
    x.AddConsumer<CodeReviewUpdatedConsumer>();
    x.AddConsumer<CiCdStatusUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
        var username = Environment.GetEnvironmentVariable("RabbitMq__Username") ?? "guest";
        var password = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:8090")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<ForgeHub>("/hubs/forge");

// Health endpoint
app.MapHealthChecks("/health");

app.MapGet("/", () => "ForgeFlow Notification Service is running!");

Log.Information("ForgeFlow Notification Service starting...");
app.Run();
