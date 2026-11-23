using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Api.Middleware;
using TaskApi.Api.Services;
using TaskApi.Application;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Infrastructure;
using TaskApi.Infrastructure.Authentication.Options;
using TaskApi.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Configuration
// --------------------------------------------------

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

// --------------------------------------------------
// Application and Infrastructure
// --------------------------------------------------

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

// --------------------------------------------------
// API Services
// --------------------------------------------------

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

// --------------------------------------------------
// Controllers and OpenAPI
// --------------------------------------------------

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new
                {
                    Field = entry.Key,
                    Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Invalid value."
                        : error.ErrorMessage
                }));

            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed",
                Detail = "One or more request fields are invalid.",
                Instance = context.HttpContext.Request.Path,
                Errors = errors
                    .GroupBy(error => error.Field)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.Message).ToArray())
            });
        };
    });

builder.Services.AddOpenApi();

// --------------------------------------------------
// Authentication - JWT
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
    // All endpoints require authentication by default.
    // Controllers/actions can opt out using [AllowAnonymous].
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

    options.AddPolicy("TaskAccess", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireRole("User", "Admin");
    });

    options.AddPolicy("CanManageAllTasks", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim(
                "permission",
                "tasks.manage.all"));
    });
});

// --------------------------------------------------
// CSRF / Antiforgery
// --------------------------------------------------

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";

    options.Cookie.Name = "__Host-TaskApi.AntiForgery";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Lax;

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
// Rate Limiting
// --------------------------------------------------

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Global rate limit: per client IP.
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 60,
                        TokensPerPeriod = 5,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));

    // Login rate limit: per client IP.
    options.AddPolicy("Login", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(context),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                SegmentsPerWindow = 5,
                AutoReplenishment = true,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("TaskApi.Security.RateLimiting");

        logger.LogWarning(
            "Rate limit exceeded. IP: {ClientIp}, Path: {Path}",
            GetClientIp(context.HttpContext),
            context.HttpContext.Request.Path);

        if (context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(
                    1,
                    (int)Math.Ceiling(retryAfter.TotalSeconds))
                .ToString();
        }

        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                message = "Too many requests. Please try again later."
            },
            cancellationToken);
    };
});

// --------------------------------------------------
// Build
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// Identity Seeding
// --------------------------------------------------

// The database must already have its required migrations applied.
await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration);

// --------------------------------------------------
// Middleware Pipeline
// --------------------------------------------------

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

// --------------------------------------------------
// OpenAPI
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --------------------------------------------------
// Endpoints
// --------------------------------------------------

app.MapControllers();

app.Run();

// --------------------------------------------------
// Helpers
// --------------------------------------------------

static string GetClientIp(HttpContext context)
{
    var ip = context.Connection.RemoteIpAddress;

    if (ip is null)
        return "unknown";

    return ip.IsIPv4MappedToIPv6
        ? ip.MapToIPv4().ToString()
        : ip.ToString();
}