using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Authorization;
using TaskApi.DTOs.Tasks;
using TaskApi.Services;

namespace TaskApi.Controllers;

[Authorize(Policy = "TaskAccess")]
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IAuthorizationService _authorizationService;

    public TasksController(
        ITaskService taskService,
        IAuthorizationService authorizationService)
    {
        _taskService = taskService;
        _authorizationService = authorizationService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException(
            "Authenticated user ID is missing.");

    private bool IsAdmin =>
        User.IsInRole("Admin") ||
        User.HasClaim("permission", "tasks.manage.all");

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TaskQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAllAsync(
            parameters,
            UserId,
            includeAll: false,
            cancellationToken);

        return Ok(result);
    }

    // Admin-only endpoint
    [HttpGet("admin/all")]
    [Authorize(Policy = "CanManageAllTasks")]
    public async Task<IActionResult> GetAllForAdmin(
        [FromQuery] TaskQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAllAsync(
            parameters,
            UserId,
            includeAll: true,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid task ID." });

        var resource = await _taskService.GetResourceByIdAsync(
            id,
            cancellationToken);

        if (resource is null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                resource,
                TaskOperations.Read);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var task = await _taskService.GetByIdAsync(
            id,
            UserId,
            IsAdmin,
            cancellationToken);

        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(
            dto,
            UserId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTaskDto dto,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid task ID." });

        var resource = await _taskService.GetResourceByIdAsync(
            id,
            cancellationToken);

        if (resource is null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                resource,
                TaskOperations.Update);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var updated = await _taskService.UpdateAsync(
            id,
            dto,
            UserId,
            IsAdmin,
            cancellationToken);

        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid task ID." });

        var resource = await _taskService.GetResourceByIdAsync(
            id,
            cancellationToken);

        if (resource is null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                resource,
                TaskOperations.Delete);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var deleted = await _taskService.DeleteAsync(
            id,
            UserId,
            IsAdmin,
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}