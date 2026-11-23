using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Application.Common.Models;
using TaskApi.Application.Features.Tasks.DTOs;
using TaskApi.Application.Features.Tasks.Specifications;

namespace TaskApi.Application.Features.Tasks.Queries.GetMyTasks;

public sealed class GetMyTasksHandler
    : IRequestHandler<
        GetMyTasksQuery,
        PagedResponse<TaskResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyTasksHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<TaskResponseDto>> Handle(
        GetMyTasksQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("An authenticated user is required.");
        }

        Validate(request);

        IQueryable<Domain.Entities.TaskItem> query =
            _context.Tasks.AsNoTracking();

        // Ordinary users see only their tasks.
        // Admins can see all tasks through this query.
        if (!_currentUser.CanManageAllTasks)
        {
            query = query.Where(t => t.OwnerId == userId);
        }

        var parameters = new TaskListParameters(
            request.Status,
            request.Priority,
            request.SortBy,
            request.Descending,
            request.PageNumber,
            request.PageSize);

        query = TaskListSpecification.Apply(
            query,
            parameters);

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await TaskListSpecification
            .ApplyPagination(query, parameters)
            .Select(t => new TaskResponseDto(
                t.Id,
                t.Title,
                t.Description,
                t.Priority,
                t.Status,
                t.DueDate,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponseDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private static void Validate(GetMyTasksQuery request)
    {
        if (request.PageNumber < 1)
        {
            throw new ArgumentException(
                "Page number must be at least 1.");
        }

        if (request.PageSize is < 1 or > 100)
        {
            throw new ArgumentException(
                "Page size must be between 1 and 100.");
        }

        if (request.Status.HasValue &&
            !Enum.IsDefined(request.Status.Value))
        {
            throw new ArgumentException(
                "Invalid task status.");
        }

        if (request.Priority.HasValue &&
            !Enum.IsDefined(request.Priority.Value))
        {
            throw new ArgumentException(
                "Invalid task priority.");
        }
    }
}