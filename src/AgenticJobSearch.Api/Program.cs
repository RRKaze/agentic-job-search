using AgenticJobSearch.Application;
using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Application.Candidates;
using AgenticJobSearch.Application.Jobs;
using AgenticJobSearch.Infrastructure;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

var api = app.MapGroup("/api");

api.MapGet("/candidate-profile", async (
    GetCandidateProfileHandler handler,
    CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(cancellationToken)));

api.MapGet("/jobs", async (
    IJobRepository jobs,
    CancellationToken cancellationToken) =>
{
    var recent = await jobs.ListRecentAsync(25, cancellationToken);
    return Results.Ok(recent.Select(job => job.ToDto()));
});

api.MapPost("/jobs", async (
    AddJobRequest request,
    AddJobHandler handler,
    CancellationToken cancellationToken) =>
{
    var validationErrors = AddJobRequestValidator.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

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

api.MapGet("/health", () => Results.Ok(new { status = "ok", service = "agentic-job-search-api" }));

app.Run();

public partial class Program
{
}
