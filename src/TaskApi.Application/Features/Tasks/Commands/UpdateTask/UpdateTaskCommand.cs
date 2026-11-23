using MediatR;
using TaskApi.Application.Features.Tasks.DTOs;
using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Application.Features.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    int Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DomainTaskStatus Status,
    DateTime? DueDate) : IRequest<TaskResponseDto?>;