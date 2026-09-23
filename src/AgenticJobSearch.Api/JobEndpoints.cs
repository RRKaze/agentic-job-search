using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Application.Jobs;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var jobs = endpoints.MapGroup("/api/jobs").RequireAuthorization();

        jobs.MapGet("/", async (IJobRepository repository, CancellationToken cancellationToken) =>
        {
            var recent = await repository.ListRecentAsync(500, cancellationToken);
            return Results.Ok(recent.Select(job => job.ToDto()));
        });

        jobs.MapPost("/", async (
            AddJobRequest request,
            AddJobHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationErrors = AddJobRequestValidator.Validate(request);
            if (validationErrors.Count > 0) return Results.ValidationProblem(validationErrors);

            try
            {
                var job = await handler.HandleAsync(request, cancellationToken);
                return Results.Created($"/api/jobs/{job.Id}", job);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        jobs.MapPut("/{id:guid}/workflow", async (
            Guid id,
            UpdateJobWorkflowRequest request,
            UpdateJobWorkflowHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.HandleAsync(id, request, cancellationToken));
            }
            catch (WorkflowFailure exception)
            {
                return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
            }
        });

        jobs.MapGet("/{id:guid}/workflow-history", async (
            Guid id,
            UpdateJobWorkflowHandler handler,
            CancellationToken cancellationToken) => Results.Ok(await handler.HistoryAsync(id, cancellationToken)));

        return endpoints;
    }
}
