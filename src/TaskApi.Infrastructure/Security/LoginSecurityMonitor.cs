using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace TaskApi.Infrastructure.Security;

public class LoginSecurityMonitor : ILoginSecurityMonitor
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LoginSecurityMonitor> _logger;

    private readonly object _lock = new();

    private static readonly TimeSpan TrackingWindow =
        TimeSpan.FromMinutes(10);

    private const int AccountThreshold = 5;
    private const int IpThreshold = 10;

    public LoginSecurityMonitor(
        IMemoryCache cache,
        ILogger<LoginSecurityMonitor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void RecordFailedLogin(
        string email,
        string ipAddress,
        string failureReason)
    {
        var accountKey = GetAccountKey(email);
        var ipKey = $"login:ip:{ipAddress}";

        int accountAttempts;
        int ipAttempts;

        lock (_lock)
        {
            accountAttempts = IncrementCounter(accountKey);
            ipAttempts = IncrementCounter(ipKey);
        }

        _logger.LogInformation(
            "Login attempt failed. Reason: {FailureReason}, AccountKey: {AccountKey}, IP: {ClientIp}",
            failureReason,
            accountKey,
            ipAddress
        );

        if (accountAttempts == AccountThreshold)
        {
            _logger.LogWarning(
                "Multiple failed logins detected for account. AccountKey: {AccountKey}, IP: {ClientIp}, Attempts: {Attempts}",
                accountKey,
                ipAddress,
                accountAttempts
            );
        }

        if (ipAttempts == IpThreshold)
        {
            _logger.LogWarning(
                "Possible brute force attack detected. IP: {ClientIp}, FailedAttempts: {Attempts}",
                ipAddress,
                ipAttempts
            );
        }
    }

    public void RecordSuccessfulLogin(
        string email,
        string ipAddress)
    {
        var accountKey = GetAccountKey(email);

        _cache.Remove(accountKey);

        _logger.LogInformation(
            "Successful login. AccountKey: {AccountKey}, IP: {ClientIp}",
            accountKey,
            ipAddress
        );
    }

    private int IncrementCounter(string key)
    {
        _cache.TryGetValue(key, out int currentCount);

        var newCount = currentCount + 1;

        _cache.Set(
            key,
            newCount,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TrackingWindow,
                Size = 1
            }
        );

        return newCount;
    }

    private static string GetAccountKey(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(normalizedEmail)
        );

        return $"login:account:{Convert.ToHexString(hash)}";
    }
}