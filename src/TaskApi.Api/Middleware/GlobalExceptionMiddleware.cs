using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Domain.Exceptions;

namespace TaskApi.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected or canceled the request.
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "An exception occurred after the response started.");

                throw;
            }

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();

            var (statusCode, title, detail) = exception switch
            {
                DomainException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        exception.Message
                    ),

                ValidationException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Validation failed",
                        exception.Message
                    ),

                ArgumentException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        exception.Message
                    ),

                UnauthorizedAccessException =>
                    (
                        StatusCodes.Status403Forbidden,
                        "Forbidden",
                        "You are not allowed to perform this operation."
                    ),

                KeyNotFoundException =>
                    (
                        StatusCodes.Status404NotFound,
                        "Resource not found",
                        "The requested resource was not found."
                    ),

                _ =>
                    (
                        StatusCodes.Status500InternalServerError,
                        "An unexpected error occurred",
                        "The server could not complete the request."
                    )
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;

            await context.Response.WriteAsJsonAsync(
                problem,
                cancellationToken: context.RequestAborted);
        }
    }
}