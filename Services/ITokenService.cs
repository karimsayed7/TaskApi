using TaskApi.Models;

namespace TaskApi.Services;

public interface ITokenService
{
    Task<AccessTokenResult> CreateAccessTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);
}

public sealed record AccessTokenResult(
    string Token,
    DateTime ExpiresAtUtc);