using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TaskApi.Domain.Enums;
using DomainTaskStatus = TaskApi.Domain.Enums.TaskStatus;

namespace TaskApi.Api.Contracts.Tasks;

public sealed class TaskQueryRequest : IValidatableObject
{
    public DomainTaskStatus? Status { get; set; }

    public TaskPriority? Priority { get; set; }

    public string SortBy { get; set; } = "createdAt";

    public bool Descending { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.Equals(
                SortBy,
                "createdAt",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                SortBy,
                "dueDate",
                StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "SortBy must be either 'createdAt' or 'dueDate'.",
                [nameof(SortBy)]);
        }
    }
}