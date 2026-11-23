using System.ComponentModel.DataAnnotations;
using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Api.Contracts.Tasks;

public sealed class UpdateTaskRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority? Priority { get; set; }

    [Required]
    [EnumDataType(typeof(DomainTaskStatus))]
    public DomainTaskStatus? Status { get; set; }

    public DateTime? DueDate { get; set; }
}