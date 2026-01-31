using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// JWT Secret'ı .env'den oku (Identity Service ile aynı key!)
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ForgeFlow.Identity";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ForgeFlow.Services";

// JWT Authentication - Token doğrulama
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claim mapping'i devre dışı bırak - sub claim olduğu gibi kalsın!
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Token'ı kimin ürettiğini doğrula
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            // Token'ın kime yönelik olduğunu doğrula
            ValidateAudience = true,
            ValidAudience = jwtAudience,

            // Token'ın süresini doğrula
            ValidateLifetime = true,

            // İmza anahtarını doğrula (en kritik!)
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            // Saat farkı toleransı (0 = kesin)
            ClockSkew = TimeSpan.Zero
        };


        // Token doğrulama event'leri (loglama için)
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[Gateway] Auth failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("sub")?.Value;
                Console.WriteLine($"[Gateway] Token validated for user: {userId}");
                return Task.CompletedTask;
            }
        };
    });

// Authorization - Policy tanımlamaları
builder.Services.AddAuthorization(options =>
{
    // Varsayılan policy: Authenticate + geçerli token
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Admin-only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Developer veya üstü
    options.AddPolicy("DeveloperOrAbove", policy =>
        policy.RequireRole("Admin", "TeamLead", "Developer"));
});

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS - Frontend erişimi için
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",      // Docker frontend
                "http://localhost:5173",      // Vite dev server
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware sırası ÇOK ÖNEMLİ!
// 0. CORS: Preflight (OPTIONS) isteklerini karşıla - EN BAŞTA olmalı!
app.UseCors();

// 1. Authentication: Token'ı doğrula, ClaimsPrincipal oluştur
app.UseAuthentication();

// 2. Authorization: Principal'a göre erişim kontrolü
app.UseAuthorization();

// 3. Custom Middleware: JWT claims'i HTTP header'larına dönüştür
// YARP {claim:sub} syntax'ı çalışmadığı için manuel inject
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // JWT'den sub claim'ini al ve header olarak ekle
        var userId = context.User.FindFirst("sub")?.Value;
        var email = context.User.FindFirst("email")?.Value;

        // Rolleri al (ClaimTypes.Role veya "role")
        var roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .Union(context.User.FindAll("role").Select(c => c.Value))
            .Distinct()
            .ToList();

        if (!string.IsNullOrEmpty(userId))
        {
            context.Request.Headers["X-User-Id"] = userId;
        }

        if (!string.IsNullOrEmpty(email))
        {
            context.Request.Headers["X-User-Email"] = email;
        }

        if (roles.Any())
        {
            context.Request.Headers["X-User-Roles"] = string.Join(",", roles);
        }
    }

    await next();
});

// 4. Reverse Proxy: İsteği backend'e yönlendir
app.MapReverseProxy();

app.Run();

