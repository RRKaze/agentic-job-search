using AgenticJobSearch.Infrastructure.Persistence;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "agentic-job-search-api" }))
            .AllowAnonymous();
        endpoints.MapGet("/api/health/live", () => Results.Ok(new { status = "ok" }))
            .AllowAnonymous();
        endpoints.MapGet("/api/health/ready", async (
            JobSearchDbContext database,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await database.Database.CanConnectAsync(cancellationToken)
                    ? Results.Ok(new { status = "ready" })
                    : Results.Json(new { status = "unavailable" }, statusCode: 503);
            }
            catch (Exception exception)
            {
                loggerFactory.CreateLogger("Readiness").LogWarning(exception, "Database readiness check failed");
                return Results.Json(new { status = "unavailable" }, statusCode: 503);
            }
        }).AllowAnonymous();
        return endpoints;
    }
}
