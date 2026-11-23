using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskApi.Application.Abstractions.Persistence;
using TaskApi.Domain.Entities;
using TaskApi.Infrastructure.Identity;
using TaskApi.Infrastructure.Persistence.Configurations;
using TaskApi.Infrastructure.Persistence.Entities;

namespace TaskApi.Infrastructure.Persistence;

public class TaskApiDbContext
    : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public TaskApiDbContext(
        DbContextOptions<TaskApiDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new TaskItemConfiguration());

        modelBuilder.ApplyConfiguration(
            new RefreshTokenConfiguration());
    }
}