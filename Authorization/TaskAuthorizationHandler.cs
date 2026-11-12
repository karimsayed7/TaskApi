using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using TaskApi.Models;

namespace TaskApi.Authorization;

public class TaskAuthorizationHandler :
    AuthorizationHandler<OperationAuthorizationRequirement, TaskItem>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        TaskItem resource)
    {
        var isAdmin =
            context.User.IsInRole("Admin") ||
            context.User.HasClaim(
                "permission",
                "tasks.manage.all");

        if (isAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId) &&
            resource.OwnerId == userId)
        {
            if (requirement.Name == TaskOperations.Read.Name ||
                requirement.Name == TaskOperations.Update.Name ||
                requirement.Name == TaskOperations.Delete.Name)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}