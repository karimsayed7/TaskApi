using Microsoft.EntityFrameworkCore;
using TaskApi.Domain.Entities;

namespace TaskApi.Application.Features.Tasks.Specifications;

public static class TaskListSpecification
{
    public static IQueryable<TaskItem> Apply(
        IQueryable<TaskItem> query,
        TaskListParameters parameters)
    {
        if (parameters.Status.HasValue)
        {
            query = query.Where(
                t => t.Status == parameters.Status.Value);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(
                t => t.Priority == parameters.Priority.Value);
        }

        query = ApplySorting(
            query,
            parameters.SortBy,
            parameters.Descending);

        return query;
    }

    private static IQueryable<TaskItem> ApplySorting(
        IQueryable<TaskItem> query,
        string sortBy,
        bool descending)
    {
        return (sortBy.Trim().ToLowerInvariant(), descending) switch
        {
            ("createdat", true) =>
                query.OrderByDescending(t => t.CreatedAtUtc)
                     .ThenByDescending(t => t.Id),

            ("createdat", false) =>
                query.OrderBy(t => t.CreatedAtUtc)
                     .ThenBy(t => t.Id),

            ("duedate", true) =>
                query.OrderByDescending(t => t.DueDate)
                     .ThenByDescending(t => t.Id),

            ("duedate", false) =>
                query.OrderBy(t => t.DueDate)
                     .ThenBy(t => t.Id),

            _ => throw new ArgumentException(
                "SortBy must be 'createdAt' or 'dueDate'.")
        };
    }

    public static IQueryable<TaskItem> ApplyPagination(
        IQueryable<TaskItem> query,
        TaskListParameters parameters)
    {
        var skip = (parameters.PageNumber - 1) *
                   parameters.PageSize;

        return query
            .Skip(skip)
            .Take(parameters.PageSize);
    }
}