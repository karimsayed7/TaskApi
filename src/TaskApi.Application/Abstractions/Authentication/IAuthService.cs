namespace TaskApi.Application.Abstractions.Authentication;

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );

    Task<AuthSession?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );

    Task<AuthSession?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    );

    Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    );
}