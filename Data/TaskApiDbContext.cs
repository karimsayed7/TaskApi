
using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data;

public class TaskApiDbContext : DbContext
{
    public TaskApiDbContext(
        DbContextOptions<TaskApiDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            // Table and primary key
            entity.ToTable("Tasks");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            // Title
            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            // Description
            entity.Property(t => t.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            // Priority
            entity.Property(t => t.Priority)
                .IsRequired();

            // Status
            entity.Property(t => t.Status)
                .IsRequired();

            // Due date
            entity.Property(t => t.DueDate)
                .IsRequired(false);

            // Creation date
            entity.Property(t => t.CreatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(t => t.Status);

            entity.HasIndex(t => t.Priority);

            entity.HasIndex(t => t.DueDate);
        });
    }
}
