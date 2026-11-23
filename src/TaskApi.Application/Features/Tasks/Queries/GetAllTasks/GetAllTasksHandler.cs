using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Application.Common.Models;
using TaskApi.Application.Features.Tasks.DTOs;
using TaskApi.Application.Features.Tasks.Specifications;

namespace TaskApi.Application.Features.Tasks.Queries.GetAllTasks;

public sealed class GetAllTasksHandler
    : IRequestHandler<
        GetAllTasksQuery,
        PagedResponse<TaskResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAllTasksHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<TaskResponseDto>> Handle(
        GetAllTasksQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.CanManageAllTasks)
        {
            throw new UnauthorizedAccessException(
                "Administrative permission is required.");
        }

        if (request.PageNumber < 1 ||
            request.PageSize is < 1 or > 100)
        {
            throw new ArgumentException(
                "Invalid pagination parameters.");
        }

        var parameters = new TaskListParameters(
            request.Status,
            request.Priority,
            request.SortBy,
            request.Descending,
            request.PageNumber,
            request.PageSize);

        var query = TaskListSpecification.Apply(
            _context.Tasks.AsNoTracking(),
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
}