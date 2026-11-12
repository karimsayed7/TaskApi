using Microsoft.AspNetCore.Identity;
using TaskApi.DTOs.Auth;

namespace TaskApi.Services;

public interface IAuthService
{
    Task<IdentityResult> RegisterAsync(
        RegisterRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<AuthSession?> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<AuthSession?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

public sealed record AuthSession(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);