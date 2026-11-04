
using Microsoft.AspNetCore.Mvc;
using TaskApi.DTOs.Tasks;
using TaskApi.Services;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TaskQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAllAsync(
            parameters, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "ID must be greater than zero."
            });
        }

        var task = await _taskService.GetByIdAsync(
            id, cancellationToken);

        if (task is null)
        {
            return NotFound(new
            {
                message = $"Task with ID {id} was not found."
            });
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(
            dto, cancellationToken);

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
        {
            return BadRequest(new
            {
                message = "ID must be greater than zero."
            });
        }

        var updated = await _taskService.UpdateAsync(
            id, dto, cancellationToken);

        if (updated is null)
        {
            return NotFound(new
            {
                message = $"Task with ID {id} was not found."
            });
        }

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "ID must be greater than zero."
            });
        }

        var deleted = await _taskService.DeleteAsync(
            id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = $"Task with ID {id} was not found."
            });
        }

        return NoContent();
    }
}
