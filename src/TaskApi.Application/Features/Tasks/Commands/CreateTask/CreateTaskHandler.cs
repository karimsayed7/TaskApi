using MediatR;
using TaskApi.Application.Abstractions.CurrentUser;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Application.Features.Tasks.DTOs;
using TaskApi.Domain.Entities;

namespace TaskApi.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskHandler
    : IRequestHandler<CreateTaskCommand, TaskResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateTaskHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TaskResponseDto> Handle(
        CreateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "Authenticated user is required.");
        }

        var task = TaskItem.Create(
            request.Title,
            request.Description,
            request.Priority,
            request.Status,
            request.DueDate,
            userId);

        _context.Tasks.Add(task);

        await _context.SaveChangesAsync(
            cancellationToken);

        return task.ToResponse();
    }
}