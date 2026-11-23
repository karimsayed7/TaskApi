using MediatR;
using TaskApi.Application.Features.Tasks.DTOs;

namespace TaskApi.Application.Features.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(int Id)
    : IRequest<TaskResponseDto?>;