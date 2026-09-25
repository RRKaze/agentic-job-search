using AgenticJobSearch.Application.Candidates;

public static class CandidateEndpoints
{
    public static IEndpointRouteBuilder MapCandidateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/candidate-profile", async (
            GetCandidateProfileHandler handler,
            CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(cancellationToken)))
            .RequireAuthorization();

        endpoints.MapPut("/api/candidate-profile", async (
            UpdateCandidateProfileRequest request,
            UpdateCandidateProfileHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.HandleAsync(request, cancellationToken));
            }
            catch (CandidateProfileFailure exception)
            {
                return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
            }
        }).RequireAuthorization();

        return endpoints;
    }
}
