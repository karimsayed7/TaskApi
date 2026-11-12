using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Authorization;
using TaskApi.Data;
using TaskApi.Middleware;
using TaskApi.Models;
using TaskApi.Options;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Configuration validation
// --------------------------------------------------

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured.");
}

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing.");
}

byte[] signingKey;

try
{
    signingKey = Convert.FromBase64String(jwtOptions.SigningKey);
}
catch (FormatException)
{
    throw new InvalidOperationException(
        "JWT signing key must be valid Base64.");
}

if (signingKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    jwtOptions.AccessTokenMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT configuration is invalid.");
}

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateOnStart();

builder.Services.Configure<RefreshTokenOptions>(
    builder.Configuration.GetSection("RefreshToken"));

// --------------------------------------------------
// Controllers and OpenAPI
// --------------------------------------------------

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// --------------------------------------------------
// Database
// --------------------------------------------------

builder.Services.AddDbContext<TaskApiDbContext>(options =>
    options.UseSqlServer(connectionString));

// --------------------------------------------------
// ASP.NET Core Identity
// --------------------------------------------------

builder.Services
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

// --------------------------------------------------
// JWT Authentication
// --------------------------------------------------

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(signingKey),

                ValidateIssuer = true,

                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,

                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType = ClaimTypes.Name,

                RoleClaimType = ClaimTypes.Role
            };
    });

// --------------------------------------------------
// Authorization
// --------------------------------------------------

builder.Services.AddAuthorization(options =>
{
    // Protect endpoints unless explicitly marked AllowAnonymous.
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

    options.AddPolicy("TaskAccess", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireRole("User", "Admin");
    });

    // Claims-based policy for administrative access.
    options.AddPolicy("CanManageAllTasks", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireClaim(
            "permission",
            "tasks.manage.all");
    });
});

builder.Services.AddSingleton<
    IAuthorizationHandler,
    TaskAuthorizationHandler>();

// --------------------------------------------------
// CSRF Protection
// --------------------------------------------------

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";

    options.Cookie.Name = "__Host-TaskApi.AntiForgery";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.None;

    options.Cookie.Path = "/";
});

// --------------------------------------------------
// CORS
// --------------------------------------------------

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0 ||
    allowedOrigins.Any(string.IsNullOrWhiteSpace))
{
    throw new InvalidOperationException(
        "At least one valid CORS origin must be configured.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --------------------------------------------------
// Application Services
// --------------------------------------------------

builder.Services.AddScoped<ITaskService, TaskService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ITokenService, TokenService>();

// --------------------------------------------------
// Build
// --------------------------------------------------

var app = builder.Build();

// Create roles and optionally provision the initial admin.
// Database migrations must be applied before startup.

await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration);

// --------------------------------------------------
// Middleware Pipeline
// --------------------------------------------------

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

// --------------------------------------------------
// OpenAPI
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();