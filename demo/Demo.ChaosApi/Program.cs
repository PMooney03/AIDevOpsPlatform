var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var mode = "healthy";

app.MapGet("/health", async (HttpContext context) =>
{
    if (mode == "slow")
    {
        await Task.Delay(TimeSpan.FromSeconds(3), context.RequestAborted);
    }

    return mode switch
    {
        "error" => Results.StatusCode(StatusCodes.Status500InternalServerError),
        "database" => Results.Json(new
        {
            status = "unhealthy",
            error = "PostgreSQL connectivity failure (DEVELOPMENT / DEMO ONLY)"
        }, statusCode: StatusCodes.Status503ServiceUnavailable),
        _ => Results.Ok(new { status = "healthy", mode, notice = "DEVELOPMENT / DEMO ONLY" })
    };
});

app.MapPost("/chaos/error", () =>
{
    mode = "error";
    return Results.Ok(new { mode, notice = "DEVELOPMENT / DEMO ONLY" });
});

app.MapPost("/chaos/slow", () =>
{
    mode = "slow";
    return Results.Ok(new { mode, notice = "DEVELOPMENT / DEMO ONLY" });
});

app.MapPost("/chaos/database", () =>
{
    mode = "database";
    return Results.Ok(new { mode, notice = "DEVELOPMENT / DEMO ONLY" });
});

app.MapPost("/chaos/recover", () =>
{
    mode = "healthy";
    return Results.Ok(new { mode, notice = "DEVELOPMENT / DEMO ONLY" });
});

app.Run();
