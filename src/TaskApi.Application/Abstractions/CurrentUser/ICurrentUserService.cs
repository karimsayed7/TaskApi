namespace TaskApi.Application.Abstractions.CurrentUser;

public interface ICurrentUserService
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    bool CanManageAllTasks { get; }
}