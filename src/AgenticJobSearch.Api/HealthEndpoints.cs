public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "agentic-job-search-api" }))
            .AllowAnonymous();
        return endpoints;
    }
}
