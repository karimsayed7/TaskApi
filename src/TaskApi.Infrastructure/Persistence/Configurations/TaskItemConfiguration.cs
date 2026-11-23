using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApi.Domain.Entities;
using TaskApi.Infrastructure.Identity;

namespace TaskApi.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration
    : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .ValueGeneratedOnAdd();

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(task => task.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(task => task.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(task => task.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(task => task.DueDate)
            .IsRequired(false);

        // Preserve the existing database column name.
        builder.Property(task => task.CreatedAtUtc)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(task => task.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(task => task.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(task => task.Status);

        builder.HasIndex(task => task.Priority);

        builder.HasIndex(task => task.DueDate);

        builder.HasIndex(task => new
        {
            task.OwnerId,
            task.CreatedAtUtc
        })
        .HasDatabaseName("IX_Tasks_OwnerId_CreatedAt");
    }
}