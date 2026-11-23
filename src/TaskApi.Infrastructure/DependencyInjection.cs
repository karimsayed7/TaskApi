using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskApi.Application.Abstractions.Authentication;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Infrastructure.Authentication;
using TaskApi.Infrastructure.Authentication.Options;
using TaskApi.Infrastructure.Identity;
using TaskApi.Infrastructure.Persistence;
using TaskApi.Infrastructure.Security;

namespace TaskApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database configuration.
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is not configured."
            );
        }

        services.AddDbContext<TaskApiDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(
                        typeof(TaskApiDbContext).Assembly.GetName().Name
                    );
                }
            )
        );

        // Application database abstraction.
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<TaskApiDbContext>()
        );

        // ASP.NET Core Identity.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;

                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);

                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<TaskApiDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // JWT options.
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(
                IsValidJwtOptions,
                "JWT configuration is invalid."
            )
            .ValidateOnStart();

        // Refresh token options.
        services
            .AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection("RefreshToken"))
            .Validate(
                options => options.ExpirationDays > 0,
                "Refresh token expiration must be greater than zero."
            )
            .ValidateOnStart();

        // Authentication services.
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IAuthService, AuthService>();

        // Security monitoring.
        services.AddMemoryCache();

        services.AddHttpContextAccessor();

        services.AddSingleton<
            ILoginSecurityMonitor,
            LoginSecurityMonitor>();

        return services;
    }

    private static bool IsValidJwtOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) ||
            string.IsNullOrWhiteSpace(options.Audience) ||
            options.AccessTokenMinutes <= 0 ||
            string.IsNullOrWhiteSpace(options.SigningKey))
        {
            return false;
        }

        try
        {
            var key = Convert.FromBase64String(options.SigningKey);

            return key.Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}