namespace TaskApi.Application.Abstractions.Authentication;

public interface ITokenService
{
    Task<AccessTokenResult> CreateAccessTokenAsync(
        string userId,
        CancellationToken cancellationToken = default
    );
}

public sealed record AccessTokenResult(
    string Token,
    DateTime ExpiresAtUtc
);