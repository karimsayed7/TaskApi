using TaskApi.DTOs.Common;
using TaskApi.DTOs.Tasks;
using TaskApi.Models;

namespace TaskApi.Services;

public interface ITaskService
{
    Task<PagedResponse<TaskResponseDto>> GetAllAsync(
        TaskQueryParameters parameters,
        string userId,
        bool includeAll,
        CancellationToken cancellationToken = default);

    Task<TaskItem?> GetResourceByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto?> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto> CreateAsync(
        CreateTaskDto dto,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<TaskResponseDto?> UpdateAsync(
        int id,
        UpdateTaskDto dto,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}