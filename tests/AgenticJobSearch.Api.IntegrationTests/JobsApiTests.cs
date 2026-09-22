using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class JobsApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Submitted_job_is_scored_persisted_and_returned()
    {
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Senior Backend Engineer",
            company = "Example Company",
            location = "Remote",
            sourceText = "Build C# and .NET backend APIs with PostgreSQL and distributed systems."
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var jobsResponse = await fixture.Client.GetAsync("/api/jobs");

        Assert.Equal(HttpStatusCode.OK, jobsResponse.StatusCode);

        using var body = JsonDocument.Parse(await jobsResponse.Content.ReadAsStringAsync());
        var jobs = body.RootElement.EnumerateArray().ToArray();
        Assert.Contains(jobs, job => job.GetProperty("title").GetString() == "Senior Backend Engineer");
    }

    [Fact]
    public async Task Tracked_application_details_are_returned_for_the_dashboard()
    {
        await fixture.SeedTrackedApplicationAsync();

        var jobs = await fixture.Client.GetFromJsonAsync<JsonElement[]>("/api/jobs");
        var tracked = Assert.Single(jobs!, job => job.GetProperty("externalId").GetString() == "JOB-DASHBOARD");

        Assert.Equal("interviewing", tracked.GetProperty("trackingStage").GetString());
        Assert.Equal("high", tracked.GetProperty("priority").GetString());
        Assert.Equal("APP-DASHBOARD", tracked.GetProperty("application").GetProperty("externalId").GetString());
        Assert.Equal("2026-09-25", tracked.GetProperty("application").GetProperty("nextFollowUp").GetString());
    }
}
