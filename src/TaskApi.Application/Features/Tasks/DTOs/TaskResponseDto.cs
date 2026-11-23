using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Application.Features.Tasks.DTOs;

public sealed record TaskResponseDto(
    int Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DomainTaskStatus Status,
    DateTime? DueDate,
    DateTime CreatedAtUtc);