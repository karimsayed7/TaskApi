using System.Security.Claims;
using TaskApi.Application.Abstractions.CurrentUser;

namespace TaskApi.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? new ClaimsPrincipal(new ClaimsIdentity());

    public string? UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated =>
        User.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        User.IsInRole("Admin");

    public bool CanManageAllTasks =>
        User.IsInRole("Admin") ||
        User.HasClaim("permission", "tasks.manage.all");
}