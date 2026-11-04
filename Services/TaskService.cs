
using Microsoft.EntityFrameworkCore;
using TaskApi.DTOs.Common;
using TaskApi.DTOs.Tasks;
using TaskApi.Models;
using TaskApi.Data;

namespace TaskApi.Services;

public class TaskService : ITaskService
{
    private readonly TaskApiDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        TaskApiDbContext context,
        ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResponse<TaskResponseDto>> GetAllAsync(
        TaskQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parameters.PageNumber < 1)
        {
            throw new ArgumentException(
                "Page number must be at least 1.");
        }

        if (parameters.PageSize is < 1 or > 100)
        {
            throw new ArgumentException(
                "Page size must be between 1 and 100.");
        }

        if (parameters.Status.HasValue &&
            !Enum.IsDefined(parameters.Status.Value))
        {
            throw new ArgumentException("Invalid task status.");
        }

        if (parameters.Priority.HasValue &&
            !Enum.IsDefined(parameters.Priority.Value))
        {
            throw new ArgumentException("Invalid task priority.");
        }

        var query = _context.Tasks
            .AsNoTracking()
            .AsQueryable();

        // Filtering
        if (parameters.Status.HasValue)
        {
            query = query.Where(
                t => t.Status == parameters.Status.Value);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(
                t => t.Priority == parameters.Priority.Value);
        }

        // Sorting
        query = parameters.SortBy.Trim().ToLowerInvariant() switch
        {
            "createdat" => parameters.Descending
                ? query.OrderByDescending(t => t.CreatedAt)
                       .ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.CreatedAt)
                       .ThenBy(t => t.Id),

            "duedate" => parameters.Descending
                ? query.OrderByDescending(t => t.DueDate)
                       .ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.DueDate)
                       .ThenBy(t => t.Id),

            _ => throw new ArgumentException(
                "SortBy must be 'createdAt' or 'dueDate'.")
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponseDto>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TaskResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TaskResponseDto> CreateAsync(
        CreateTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateTitle(dto.Title);

        if (dto.Priority is null ||
            !Enum.IsDefined(dto.Priority.Value))
        {
            throw new ArgumentException("Invalid task priority.");
        }

        if (dto.Status is null ||
            !Enum.IsDefined(dto.Status.Value))
        {
            throw new ArgumentException("Invalid task status.");
        }

        var entity = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = NormalizeDescription(dto.Description),
            Priority = dto.Priority.Value,
            Status = dto.Status.Value,
            DueDate = dto.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created task {TaskId}", entity.Id);

        return MapToResponse(entity);
    }

    public async Task<TaskResponseDto?> UpdateAsync(
        int id,
        UpdateTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateTitle(dto.Title);

        if (dto.Priority is null ||
            !Enum.IsDefined(dto.Priority.Value))
        {
            throw new ArgumentException("Invalid task priority.");
        }

        if (dto.Status is null ||
            !Enum.IsDefined(dto.Status.Value))
        {
            throw new ArgumentException("Invalid task status.");
        }

        var entity = await _context.Tasks
            .FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Title = dto.Title.Trim();
        entity.Description = NormalizeDescription(dto.Description);
        entity.Priority = dto.Priority.Value;
        entity.Status = dto.Status.Value;
        entity.DueDate = dto.DueDate;

        // Do not change Id or CreatedAt during an update.
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated task {TaskId}", entity.Id);

        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Tasks
            .FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _context.Tasks.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted task {TaskId}", id);

        return true;
    }

    private static void ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Title cannot be empty or whitespace.");
        }

        if (title.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Title cannot exceed 100 characters.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    private static TaskResponseDto MapToResponse(TaskItem entity)
    {
        return new TaskResponseDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Priority = entity.Priority,
            Status = entity.Status,
            DueDate = entity.DueDate,
            CreatedAt = entity.CreatedAt
        };
    }
}
