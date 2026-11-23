using MediatR;
using TaskApi.Application.Features.Tasks.DTOs;
using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Application.Features.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    DomainTaskStatus Status,
    DateTime? DueDate) : IRequest<TaskResponseDto>;