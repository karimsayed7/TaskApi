
using TaskApi.DTOs.Common;
using TaskApi.DTOs.Tasks;

namespace TaskApi.Services;

public interface ITaskService
{
    Task<PagedResponse<TaskResponseDto>> GetAllAsync(
        TaskQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto> CreateAsync(
        CreateTaskDto dto,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto?> UpdateAsync(
        int id,
        UpdateTaskDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
