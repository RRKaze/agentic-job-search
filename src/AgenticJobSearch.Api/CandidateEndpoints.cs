using AgenticJobSearch.Application.Candidates;

public static class CandidateEndpoints
{
    public static IEndpointRouteBuilder MapCandidateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/candidate-profile", async (
            GetCandidateProfileHandler handler,
            CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(cancellationToken)))
            .RequireAuthorization();

        return endpoints;
    }
}
