using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Application.Features.Tasks.DTOs;

namespace TaskApi.Application.Features.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdHandler
    : IRequestHandler<GetTaskByIdQuery, TaskResponseDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTaskByIdHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TaskResponseDto?> Handle(
        GetTaskByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException();
        }

        var query = _context.Tasks
            .AsNoTracking()
            .Where(t => t.Id == request.Id);

        if (!_currentUser.CanManageAllTasks)
        {
            query = query.Where(t => t.OwnerId == userId);
        }

        var task = await query.FirstOrDefaultAsync(
            cancellationToken);

        return task?.ToResponse();
    }
}