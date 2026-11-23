using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Api.Contracts.Tasks;
using TaskApi.Api.Mapping;
using TaskApi.Application.Features.Tasks.Commands.CreateTask;
using TaskApi.Application.Features.Tasks.Commands.DeleteTask;
using TaskApi.Application.Features.Tasks.Commands.UpdateTask;
using TaskApi.Application.Features.Tasks.Queries.GetAllTasks;
using TaskApi.Application.Features.Tasks.Queries.GetMyTasks;
using TaskApi.Application.Features.Tasks.Queries.GetTaskById;

namespace TaskApi.Api.Controllers;

[Authorize(Policy = "TaskAccess")]
[ApiController]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly ISender _sender;

    public TasksController(ISender sender)
    {
        _sender = sender;
    }

    // GET: /api/tasks
    // Returns tasks owned by the authenticated user.
    [HttpGet]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] TaskQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetMyTasksQuery(
            request.Status,
            request.Priority,
            request.SortBy.ToLowerInvariant(),
            request.Descending,
            request.PageNumber,
            request.PageSize
        );

        var result = await _sender.Send(
            query,
            cancellationToken);

        return Ok(result);
    }

    // GET: /api/tasks/admin/all
    // Returns all tasks. Requires administrative permission.
    [HttpGet("admin/all")]
    [Authorize(Policy = "CanManageAllTasks")]
    public async Task<IActionResult> GetAllTasks(
        [FromQuery] TaskQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllTasksQuery(
            request.Status,
            request.Priority,
            request.SortBy.ToLowerInvariant(),
            request.Descending,
            request.PageNumber,
            request.PageSize
        );

        var result = await _sender.Send(
            query,
            cancellationToken);

        return Ok(result);
    }

    // GET: /api/tasks/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Invalid task ID."
            });
        }

        var result = await _sender.Send(
            new GetTaskByIdQuery(id),
            cancellationToken);

        // Missing tasks and tasks owned by another user
        // both return NotFound.
        return result is null
            ? NotFound()
            : Ok(result);
    }

    // POST: /api/tasks
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            request.Priority!.Value,
            request.Status!.Value,
            request.DueDate.ToUtc()
        );

        var created = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // PUT: /api/tasks/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Invalid task ID."
            });
        }

        var command = new UpdateTaskCommand(
            id,
            request.Title,
            request.Description,
            request.Priority!.Value,
            request.Status!.Value,
            request.DueDate.ToUtc()
        );

        var updated = await _sender.Send(
            command,
            cancellationToken);

        return updated is null
            ? NotFound()
            : Ok(updated);
    }

    // DELETE: /api/tasks/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Invalid task ID."
            });
        }

        var deleted = await _sender.Send(
            new DeleteTaskCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}