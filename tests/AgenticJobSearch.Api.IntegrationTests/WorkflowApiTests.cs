using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class WorkflowApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Saved_job_moves_to_applications_and_records_history()
    {
        var job = await CreateJob(fixture.Client, "Workflow job");
        var id = job.GetProperty("id").GetGuid();

        var applied = await fixture.Client.PutAsJsonAsync($"/api/jobs/{id}/workflow", new
        {
            status = "applied",
            statusDate = "2026-09-22",
            submittedDate = "2026-09-21",
            resumeVersion = "backend-v2",
            nextFollowUp = "2026-09-29",
            notes = "Applied through the company site."
        });

        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);
        var result = await applied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("applied", result.GetProperty("trackingStage").GetString());
        Assert.Equal("Submitted", result.GetProperty("application").GetProperty("state").GetString());
        Assert.Equal("2026-09-21", result.GetProperty("application").GetProperty("submittedDate").GetString());

        var interviewing = await fixture.Client.PutAsJsonAsync($"/api/jobs/{id}/workflow", new
        {
            status = "interviewing",
            statusDate = "2026-09-24",
            submittedDate = "2026-09-21",
            resumeVersion = "backend-v2",
            nextFollowUp = "2026-09-30",
            outcome = "Recruiter screen scheduled",
            notes = "Thirty minute call."
        });
        Assert.Equal(HttpStatusCode.OK, interviewing.StatusCode);

        var history = await fixture.Client.GetFromJsonAsync<JsonElement[]>($"/api/jobs/{id}/workflow-history");
        Assert.Equal(2, history!.Length);
        Assert.Equal("interviewing", history[0].GetProperty("currentStatus").GetString());
        Assert.Equal("lead", history[1].GetProperty("previousStatus").GetString());
    }

    [Fact]
    public async Task Invalid_transition_and_closed_follow_up_are_rejected_without_changes()
    {
        var job = await CreateJob(fixture.Client, "Guarded workflow job");
        var id = job.GetProperty("id").GetGuid();

        var skipped = await fixture.Client.PutAsJsonAsync($"/api/jobs/{id}/workflow", new
        {
            status = "offer",
            statusDate = "2026-09-22"
        });
        Assert.Equal(HttpStatusCode.Conflict, skipped.StatusCode);

        var applied = await fixture.Client.PutAsJsonAsync($"/api/jobs/{id}/workflow", new
        {
            status = "applied",
            statusDate = "2026-09-22"
        });
        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);

        var rejected = await fixture.Client.PutAsJsonAsync($"/api/jobs/{id}/workflow", new
        {
            status = "rejected",
            statusDate = "2026-09-23",
            nextFollowUp = "2026-09-30"
        });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var persisted = Assert.Single((await fixture.Client.GetFromJsonAsync<JsonElement[]>("/api/jobs"))!, item => item.GetProperty("id").GetGuid() == id);
        Assert.Equal("applied", persisted.GetProperty("trackingStage").GetString());
    }

    [Fact]
    public async Task Another_account_cannot_update_or_read_workflow_history()
    {
        var job = await CreateJob(fixture.Client, "Private workflow job");
        var id = job.GetProperty("id").GetGuid();
        using var other = fixture.CreateClient();
        var registration = await other.PostAsJsonAsync("/api/account/register", new
        {
            displayName = "Another Person",
            email = $"workflow-{Guid.NewGuid():N}@example.test",
            password = "Fictional test passphrase 2026"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var update = await other.PutAsJsonAsync($"/api/jobs/{id}/workflow", new
        {
            status = "applied",
            statusDate = "2026-09-22"
        });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<JsonElement[]>($"/api/jobs/{id}/workflow-history"))!);
    }

    private static async Task<JsonElement> CreateJob(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            title,
            company = "Example Company",
            location = "Remote",
            sourceText = "Build and maintain backend services with C# and PostgreSQL."
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
