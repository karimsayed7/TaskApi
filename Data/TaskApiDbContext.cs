using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data;

public class TaskApiDbContext
    : IdentityDbContext<ApplicationUser>
{
    public TaskApiDbContext(
        DbContextOptions<TaskApiDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(t => t.Priority)
                .IsRequired();

            entity.Property(t => t.Status)
                .IsRequired();

            entity.Property(t => t.DueDate)
                .IsRequired(false);

            entity.Property(t => t.CreatedAt)
                .IsRequired();

            entity.HasIndex(t => t.Status);

            entity.HasIndex(t => t.Priority);

            entity.HasIndex(t => t.DueDate);

            entity.HasIndex(t => new
            {
                t.OwnerId,
                t.CreatedAt
            });

            entity.HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(t => t.UserId)
                .IsRequired();

            entity.Property(t => t.ReplacedByTokenHash)
                .HasMaxLength(64);

            entity.Property(t => t.RowVersion)
                .IsRowVersion();

            entity.HasIndex(t => t.TokenHash)
                .IsUnique();

            entity.HasIndex(t => t.TokenFamilyId);

            entity.HasIndex(t => t.UserId);

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}