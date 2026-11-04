
using System.ComponentModel.DataAnnotations;
using TaskApi.Models;

namespace TaskApi.DTOs.Tasks;

public class TaskQueryParameters
{
    public TaskStatuss? Status { get; set; }

    public TaskPriority? Priority { get; set; }

    public string SortBy { get; set; } = "createdAt";

    public bool Descending { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
