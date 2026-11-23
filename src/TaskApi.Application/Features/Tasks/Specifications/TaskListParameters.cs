using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Application.Features.Tasks.Specifications;

public sealed record TaskListParameters(
    DomainTaskStatus? Status,
    TaskPriority? Priority,
    string SortBy,
    bool Descending,
    int PageNumber,
    int PageSize);