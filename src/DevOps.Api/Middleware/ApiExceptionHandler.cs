using DevOps.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = exception switch
        {
            DomainValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validation.Errors),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                exception.Message,
                (IReadOnlyDictionary<string, string[]>?)null),
            ConflictException => (
                StatusCodes.Status409Conflict,
                exception.Message,
                null),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                exception.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected for {Path} with {StatusCode}", httpContext.Request.Path, statusCode);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = "about:blank",
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
