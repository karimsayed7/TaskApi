namespace TaskApi.Services.Security;

public interface ILoginSecurityMonitor
{
    void RecordFailedLogin(
        string email,
        string ipAddress,
        string failureReason);

    void RecordSuccessfulLogin(
        string email,
        string ipAddress);
}