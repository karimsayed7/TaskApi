using MediatR;
using TaskApi.Application.Common.Models;
using TaskApi.Application.Features.Tasks.DTOs;
using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Application.Features.Tasks.Queries.GetAllTasks;

public sealed record GetAllTasksQuery(
    DomainTaskStatus? Status,
    TaskPriority? Priority,
    string SortBy = "createdAt",
    bool Descending = true,
    int PageNumber = 1,
    int PageSize = 10)
    : IRequest<PagedResponse<TaskResponseDto>>;