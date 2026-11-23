using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using TaskApi.Application.Abstractions.Authentication;
using TaskApi.Infrastructure.Authentication.Options;
using TaskApi.Infrastructure.Identity;
using TaskApi.Infrastructure.Persistence;
using TaskApi.Infrastructure.Persistence.Entities;
using TaskApi.Infrastructure.Security;

namespace TaskApi.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TaskApiDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly RefreshTokenOptions _refreshOptions;
    private readonly ILoginSecurityMonitor _securityMonitor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TaskApiDbContext context,
        ITokenService tokenService,
        IOptions<RefreshTokenOptions> refreshOptions,
        ILoginSecurityMonitor securityMonitor,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _tokenService = tokenService;
        _refreshOptions = refreshOptions.Value;
        _securityMonitor = securityMonitor;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        email = email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var createResult = await _userManager.CreateAsync(
            user,
            password
        );

        if (!createResult.Succeeded)
        {
            return new RegistrationResult(
                false,
                createResult.Errors
                    .Select(error => new RegistrationError(
                        error.Code,
                        error.Description))
                    .ToList()
            );
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "User"
        );

        if (!roleResult.Succeeded)
        {
            var cleanupResult =
                await _userManager.DeleteAsync(user);

            if (!cleanupResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "User registration failed and the temporary user could not be removed."
                );
            }

            return new RegistrationResult(
                false,
                roleResult.Errors
                    .Select(error => new RegistrationError(
                        error.Code,
                        error.Description))
                    .ToList()
            );
        }

        return new RegistrationResult(
            true,
            []
        );
    }

    public async Task<AuthSession?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var clientIp = GetClientIpAddress();

        email = email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            _securityMonitor.RecordFailedLogin(
                email,
                clientIp,
                "UnknownAccount"
            );

            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true
        );

        if (!result.Succeeded)
        {
            var failureReason = result.IsLockedOut
                ? "AccountLockedOut"
                : result.IsNotAllowed
                    ? "LoginNotAllowed"
                    : "InvalidCredentials";

            _securityMonitor.RecordFailedLogin(
                email,
                clientIp,
                failureReason
            );

            return null;
        }

        _securityMonitor.RecordSuccessfulLogin(
            email,
            clientIp
        );

        return await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            cancellationToken
        );
    }

    public async Task<AuthSession?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken
            );

        var tokenHash = HashToken(refreshToken);

        var existingToken = await _context.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken
            );

        if (existingToken is null)
            return null;

        var now = DateTime.UtcNow;

        // A previously revoked token was reused.
        if (existingToken.RevokedAtUtc.HasValue)
        {
            await RevokeFamilyAsync(
                existingToken.TokenFamilyId,
                now,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        // Expired refresh token.
        if (existingToken.ExpiresAtUtc <= now)
        {
            existingToken.RevokedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        // Do not allow locked-out accounts to refresh sessions.
        if (await _userManager.IsLockedOutAsync(existingToken.User))
        {
            await RevokeFamilyAsync(
                existingToken.TokenFamilyId,
                now,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        var newRawToken = GenerateRefreshToken();
        var newTokenHash = HashToken(newRawToken);

        var accessToken = await _tokenService.CreateAccessTokenAsync(
            existingToken.UserId,
            cancellationToken
        );

        var newRefreshToken = new RefreshToken
        {
            TokenHash = newTokenHash,
            UserId = existingToken.UserId,
            TokenFamilyId = existingToken.TokenFamilyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(
                _refreshOptions.ExpirationDays
            )
        };

        existingToken.RevokedAtUtc = now;
        existingToken.ReplacedByTokenHash = newTokenHash;

        _context.RefreshTokens.Add(newRefreshToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);

            return null;
        }

        return new AuthSession(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            newRawToken,
            newRefreshToken.ExpiresAtUtc
        );
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        var token = await _context.RefreshTokens
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken
            );

        if (token is null)
            return;

        await RevokeFamilyAsync(
            token.TokenFamilyId,
            DateTime.UtcNow,
            cancellationToken
        );
    }

    private async Task<AuthSession> CreateSessionAsync(
        ApplicationUser user,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var rawRefreshToken = GenerateRefreshToken();

        var now = DateTime.UtcNow;

        var refreshToken = new RefreshToken
        {
            TokenHash = HashToken(rawRefreshToken),
            UserId = user.Id,
            TokenFamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(
                _refreshOptions.ExpirationDays
            )
        };

        var accessToken = await _tokenService.CreateAccessTokenAsync(
            user.Id,
            cancellationToken
        );

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthSession(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshToken.ExpiresAtUtc
        );
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token =>
                token.TokenFamilyId == familyId &&
                token.RevokedAtUtc == null
            )
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = revokedAt;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(token)
        );

        return Convert.ToHexString(hash);
    }

    private string GetClientIpAddress()
    {
        var ip = _httpContextAccessor
            .HttpContext?
            .Connection
            .RemoteIpAddress;

        if (ip is null)
            return "unknown";

        return ip.IsIPv4MappedToIPv6
            ? ip.MapToIPv4().ToString()
            : ip.ToString();
    }
}