using Microsoft.EntityFrameworkCore;
using TaskApi.Domain.Entities;

namespace TaskApi.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<TaskItem> Tasks { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}