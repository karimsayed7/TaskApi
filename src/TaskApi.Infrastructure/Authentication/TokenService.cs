using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Application.Abstractions.Authentication;
using TaskApi.Infrastructure.Authentication.Options;
using TaskApi.Infrastructure.Identity;

namespace TaskApi.Infrastructure.Authentication;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<JwtOptions> options)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options.Value;

        var key = Convert.FromBase64String(_options.SigningKey);

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256
        );
    }

    public async Task<AccessTokenResult> CreateAccessTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new InvalidOperationException(
                "Cannot create an access token for a missing user.");
        }

        var now = DateTime.UtcNow;

        var expiresAt = now.AddMinutes(
            _options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id
            ),

            new(
                ClaimTypes.Name,
                user.UserName ?? user.Email ?? user.Id
            ),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()
            ),

            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now)
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64
            )
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(
                new Claim(ClaimTypes.Email, user.Email)
            );
        }

        // User roles.
        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, role)
            );

            // Include claims assigned to each role.
            var identityRole =
                await _roleManager.FindByNameAsync(role);

            if (identityRole is null)
                continue;

            var roleClaims =
                await _roleManager.GetClaimsAsync(identityRole);

            claims.AddRange(roleClaims);
        }

        // Individual user claims.
        var userClaims =
            await _userManager.GetClaimsAsync(user);

        claims.AddRange(userClaims);

        // Remove duplicate claims.
        claims = claims
            .GroupBy(claim => new
            {
                claim.Type,
                claim.Value
            })
            .Select(group => group.First())
            .ToList();

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _credentials
        );

        var tokenString =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(
            tokenString,
            expiresAt
        );
    }
}