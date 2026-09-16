using System.Diagnostics;

namespace DevOps.Api.Middleware;

public sealed class HttpRequestLoggingMiddleware(RequestDelegate next, ILogger<HttpRequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        await next(context);
        var durationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        logger.LogInformation(
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs} ms",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            (long)durationMs);
    }

    private static bool ShouldSkip(PathString path)
    {
        return path.StartsWithSegments("/metrics")
            || path.StartsWithSegments("/health")
            || path.StartsWithSegments("/swagger");
    }
}
