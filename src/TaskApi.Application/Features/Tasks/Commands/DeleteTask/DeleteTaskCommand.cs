using MediatR;

namespace TaskApi.Application.Features.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(int Id)
    : IRequest<bool>;