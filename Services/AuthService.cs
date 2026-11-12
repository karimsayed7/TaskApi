using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskApi.Data;
using TaskApi.DTOs.Auth;
using TaskApi.Models;
using TaskApi.Options;
using System.Data;
namespace TaskApi.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TaskApiDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly RefreshTokenOptions _refreshOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TaskApiDbContext context,
        ITokenService tokenService,
        IOptions<RefreshTokenOptions> refreshOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _tokenService = tokenService;
        _refreshOptions = refreshOptions.Value;
    }

    public async Task<IdentityResult> RegisterAsync(
        RegisterRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(
            user,
            dto.Password);

        if (!result.Succeeded)
            return result;

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "User");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return roleResult;
        }

        return IdentityResult.Success;
    }

    public async Task<AuthSession?> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        if (user is null)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            dto.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
            return null;

        return await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            cancellationToken);
    }

    public async Task<AuthSession?> RefreshAsync(
    string refreshToken,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var tokenHash = HashToken(refreshToken);

        var existingToken = await _context.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken);

        if (existingToken is null)
            return null;

        var now = DateTime.UtcNow;

        if (existingToken.RevokedAtUtc.HasValue)
        {
            await RevokeFamilyAsync(
                existingToken.TokenFamilyId,
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        if (existingToken.ExpiresAtUtc <= now)
        {
            existingToken.RevokedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        if (await _userManager.IsLockedOutAsync(existingToken.User))
        {
            await RevokeFamilyAsync(
                existingToken.TokenFamilyId,
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        var newRawToken = GenerateRefreshToken();
        var newTokenHash = HashToken(newRawToken);

        var accessToken = await _tokenService.CreateAccessTokenAsync(
            existingToken.User,
            cancellationToken);

        var newRefreshToken = new RefreshToken
        {
            TokenHash = newTokenHash,
            UserId = existingToken.UserId,
            TokenFamilyId = existingToken.TokenFamilyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(
                _refreshOptions.ExpirationDays)
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
            newRefreshToken.ExpiresAtUtc);
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        var token = await _context.RefreshTokens
            .SingleOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken);

        if (token is null)
            return;

        await RevokeFamilyAsync(
            token.TokenFamilyId,
            DateTime.UtcNow,
            cancellationToken);
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
                _refreshOptions.ExpirationDays)
        };

        var accessToken = await _tokenService.CreateAccessTokenAsync(
            user,
            cancellationToken);

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthSession(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshToken.ExpiresAtUtc);
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t =>
                t.TokenFamilyId == familyId &&
                t.RevokedAtUtc == null)
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
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hash);
    }
}