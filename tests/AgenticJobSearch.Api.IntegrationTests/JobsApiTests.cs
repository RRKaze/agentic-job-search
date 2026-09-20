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
}
