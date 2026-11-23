
using TaskApi.Domain.Enums;
using TaskApi.Domain.Exceptions;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Domain.Entities;

public class TaskItem
{
    public int Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DomainTaskStatus Status { get; private set; }

    public DateTime? DueDate { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    // EF Core
    private TaskItem()
    {
    }

    private TaskItem(
        string title,
        string? description,
        TaskPriority priority,
        DomainTaskStatus status,
        DateTime? dueDate,
        string ownerId)
    {
        SetTitle(title);
        SetDescription(description);
        SetPriority(priority);
        SetStatus(status);

        if (dueDate.HasValue &&
            dueDate.Value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException(
                "Due date must be provided in UTC.");
        }

        DueDate = dueDate;

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new DomainException(
                "Task owner is required.");
        }

        OwnerId = ownerId;

        CreatedAtUtc = DateTime.UtcNow;
    }

    public static TaskItem Create(
        string title,
        string? description,
        TaskPriority priority,
        DomainTaskStatus status,
        DateTime? dueDate,
        string ownerId)
    {
        return new TaskItem(
            title,
            description,
            priority,
            status,
            dueDate,
            ownerId);
    }

    public void Update(
        string title,
        string? description,
        TaskPriority priority,
        DomainTaskStatus status,
        DateTime? dueDate)
    {
        SetTitle(title);
        SetDescription(description);
        SetPriority(priority);
        SetStatus(status);

        if (dueDate.HasValue &&
            dueDate.Value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException(
                "Due date must be provided in UTC.");
        }

        DueDate = dueDate;
    }

    public void ChangeStatus(DomainTaskStatus status)
    {
        SetStatus(status);
    }

    public void ChangePriority(TaskPriority priority)
    {
        SetPriority(priority);
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException(
                "Task title cannot be empty.");
        }

        title = title.Trim();

        if (title.Length > 100)
        {
            throw new DomainException(
                "Task title cannot exceed 100 characters.");
        }

        Title = title;
    }

    private void SetDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            return;
        }

        description = description.Trim();

        if (description.Length > 500)
        {
            throw new DomainException(
                "Task description cannot exceed 500 characters.");
        }

        Description = description;
    }

    private void SetPriority(TaskPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new DomainException(
                "Invalid task priority.");
        }

        Priority = priority;
    }

    private void SetStatus(DomainTaskStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException(
                "Invalid task status.");
        }

        Status = status;
    }
}
