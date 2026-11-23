using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Application.Features.Tasks.DTOs;

namespace TaskApi.Application.Features.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskHandler
    : IRequestHandler<UpdateTaskCommand, TaskResponseDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateTaskHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TaskResponseDto?> Handle(
        UpdateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException();
        }

        var query = _context.Tasks
            .Where(t => t.Id == request.Id);

        if (!_currentUser.CanManageAllTasks)
        {
            query = query.Where(t => t.OwnerId == userId);
        }

        var task = await query.FirstOrDefaultAsync(
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        task.Update(
            request.Title,
            request.Description,
            request.Priority,
            request.Status,
            request.DueDate);

        await _context.SaveChangesAsync(
            cancellationToken);

        return task.ToResponse();
    }
}