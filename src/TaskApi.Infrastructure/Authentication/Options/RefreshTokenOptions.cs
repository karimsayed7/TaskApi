namespace TaskApi.Infrastructure.Authentication.Options;

public class RefreshTokenOptions
{
    public int ExpirationDays { get; set; } = 7;
}