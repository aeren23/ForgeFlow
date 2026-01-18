using ForgeFlow.Identity.Application.Abstractions;
using ForgeFlow.Identity.Domain.Entities;
using ForgeFlow.Identity.Infrastructure.Persistence;
using ForgeFlow.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFlow.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<IdentityDbContext>(opt => opt.UseSqlServer(connectionString));

        // ASP.NET Core Identity with custom ApplicationRole
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password policy
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // User settings
                options.User.RequireUniqueEmail = true;

                // Lockout settings (brute force koruması)
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        // Token Service
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}

