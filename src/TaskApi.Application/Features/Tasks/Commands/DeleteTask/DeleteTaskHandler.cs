using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Application.Abstractions.Persistence;

namespace TaskApi.Application.Features.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskHandler
    : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteTaskHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(
        DeleteTaskCommand request,
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
            return false;
        }

        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}