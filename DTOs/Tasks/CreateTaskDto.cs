
using System.ComponentModel.DataAnnotations;
using TaskApi.Models;

namespace TaskApi.DTOs.Tasks;

public class CreateTaskDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority? Priority { get; set; }

    [Required]
    [EnumDataType(typeof(TaskStatuss))]
    public TaskStatuss? Status { get; set; }

    public DateTime? DueDate { get; set; }
}
