using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class JobEvaluationApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Reevaluation_preserves_ordered_versioned_profile_snapshots()
    {
        await SaveProfileAsync("Senior Engineer");
        var created = await CreateJobAsync();
        var jobId = created.GetProperty("id").GetGuid();

        await SaveProfileAsync("Staff Engineer");
        var reevaluate = await fixture.Client.PostAsync($"/api/jobs/{jobId}/evaluations", null);

        Assert.Equal(HttpStatusCode.Created, reevaluate.StatusCode);
        var history = await fixture.Client.GetFromJsonAsync<JsonElement[]>($"/api/jobs/{jobId}/evaluations");
        Assert.NotNull(history);
        Assert.Equal(2, history.Length);
        Assert.All(history, evaluation => Assert.Equal("deterministic-v1", evaluation.GetProperty("scoringVersion").GetString()));
        Assert.Equal("Staff Engineer", history[0].GetProperty("profileSnapshot").GetProperty("targetLevel").GetString());
        Assert.Equal("Senior Engineer", history[1].GetProperty("profileSnapshot").GetProperty("targetLevel").GetString());
        Assert.True(history[0].GetProperty("evaluatedAt").GetDateTimeOffset() >= history[1].GetProperty("evaluatedAt").GetDateTimeOffset());

        var jobs = await fixture.Client.GetFromJsonAsync<JsonElement[]>("/api/jobs");
        var current = Assert.Single(jobs!, job => job.GetProperty("id").GetGuid() == jobId);
        Assert.Contains("Staff Engineer", current.GetProperty("explanation").GetString());
    }

    [Fact]
    public async Task Evaluation_factors_snapshot_only_verified_supporting_evidence()
    {
        await SaveProfileAsync("Senior Engineer");
        var created = await CreateJobAsync();
        var jobId = created.GetProperty("id").GetGuid();

        var history = await fixture.Client.GetFromJsonAsync<JsonElement[]>($"/api/jobs/{jobId}/evaluations");
        var evaluation = Assert.Single(history!);
        var dotnetFactor = Assert.Single(
            evaluation.GetProperty("factors").EnumerateArray(),
            factor => factor.GetProperty("name").GetString() == "C#/.NET match");
        var evidence = Assert.Single(dotnetFactor.GetProperty("evidence").EnumerateArray());

        Assert.Equal("Built C# and .NET backend APIs.", evidence.GetProperty("statement").GetString());
        Assert.Equal("Resume", evidence.GetProperty("source").GetString());
        Assert.DoesNotContain("Unconfirmed Kubernetes ownership.", evaluation.GetRawText());
    }

    [Fact]
    public async Task Evaluation_history_is_owner_scoped()
    {
        var created = await CreateJobAsync();
        var jobId = created.GetProperty("id").GetGuid();
        using var other = fixture.CreateClient();
        var register = await other.PostAsJsonAsync("/api/account/register", new
        {
            displayName = "Other Owner",
            email = $"evaluation-{Guid.NewGuid():N}@example.test",
            password = "Fictional evaluation passphrase 2026"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/jobs/{jobId}/evaluations", null)).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<JsonElement[]>($"/api/jobs/{jobId}/evaluations"))!);
    }

    private async Task<JsonElement> CreateJobAsync()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Senior Backend Engineer",
            company = "Example Company",
            location = "Remote",
            sourceText = "Build C# and .NET backend APIs with PostgreSQL and distributed systems."
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task SaveProfileAsync(string targetLevel)
    {
        var response = await fixture.Client.PutAsJsonAsync("/api/candidate-profile", new
        {
            headline = "Backend engineer",
            professionalSummary = "Builds reliable services.",
            targetLevel,
            targetRoleFamilies = "Backend, Platform",
            targetIndustries = "Technology",
            preferredLocations = "Remote",
            workModePreference = "remote",
            employmentTypePreference = "full_time",
            skills = "C#, .NET, PostgreSQL",
            workAuthorization = "Authorized",
            requiresSponsorship = false,
            linkedInUrl = "",
            portfolioUrl = "",
            resumeText = "Fictional resume",
            institution = "",
            degree = "",
            fieldOfStudy = "",
            graduationYear = (int?)null,
            yearsExperience = 8,
            recentEmployer = "Example Corp",
            recentJobTitle = "Engineer",
            evidence = new[]
            {
                new { category = "Backend", statement = "Built C# and .NET backend APIs.", verificationStatus = "Verified", source = "Resume" },
                new { category = "Draft", statement = "Unconfirmed Kubernetes ownership.", verificationStatus = "Unverified", source = "Notes" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
