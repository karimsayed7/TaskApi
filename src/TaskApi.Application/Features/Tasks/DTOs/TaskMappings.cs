using TaskApi.Domain.Entities;

namespace TaskApi.Application.Features.Tasks.DTOs;

public static class TaskMappings
{
    public static TaskResponseDto ToResponse(
        this TaskItem task)
    {
        return new TaskResponseDto(
            task.Id,
            task.Title,
            task.Description,
            task.Priority,
            task.Status,
            task.DueDate,
            task.CreatedAtUtc);
    }
}