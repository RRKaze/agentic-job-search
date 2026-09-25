using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class CandidateProfileApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Profile_update_persists_general_background_and_verified_evidence()
    {
        using var client = fixture.CreateClient();
        await Register(client, "profile@example.test", "experienced_worker");

        var response = await client.PutAsJsonAsync("/api/candidate-profile", ProfileUpdate());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Program Manager", saved.GetProperty("targetRoleFamilies").GetString());
        Assert.Equal("Community Health Network", saved.GetProperty("recentEmployer").GetString());
        Assert.Equal("Verified", saved.GetProperty("evidence")[0].GetProperty("verificationStatus").GetString());

        var persisted = await client.GetFromJsonAsync<JsonElement>("/api/candidate-profile");
        Assert.Equal("Operations", persisted.GetProperty("skills").GetString());
        Assert.True(persisted.GetProperty("requiresSponsorship").GetBoolean());
        Assert.Single(persisted.GetProperty("evidence").EnumerateArray());

        var revised = ProfileUpdate() with
        {
            Evidence = [new("Leadership", "Revised documented accomplishment.", "NeedsReview", "Manager feedback")]
        };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/candidate-profile", revised)).StatusCode);
        var revision = await client.GetFromJsonAsync<JsonElement>("/api/candidate-profile");
        Assert.Equal("Leadership", revision.GetProperty("evidence")[0].GetProperty("category").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/candidate-profile", ProfileUpdate() with { Evidence = [] })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/candidate-profile")).GetProperty("evidence").EnumerateArray());
    }

    [Fact]
    public async Task Profiles_are_owner_scoped()
    {
        using var first = fixture.CreateClient();
        using var second = fixture.CreateClient();
        await Register(first, "profile-first@example.test", "experienced_worker");
        await Register(second, "profile-second@example.test", "new_graduate");

        Assert.Equal(HttpStatusCode.OK, (await first.PutAsJsonAsync("/api/candidate-profile", ProfileUpdate())).StatusCode);

        var untouched = await second.GetFromJsonAsync<JsonElement>("/api/candidate-profile");
        Assert.Equal("", untouched.GetProperty("targetRoleFamilies").GetString());
        Assert.Empty(untouched.GetProperty("evidence").EnumerateArray());
    }

    [Fact]
    public async Task Invalid_urls_years_and_missing_csrf_header_are_rejected()
    {
        using var client = fixture.CreateClient();
        await Register(client, "profile-validation@example.test", "new_graduate");
        var invalid = ProfileUpdate() with { LinkedInUrl = "javascript:alert(1)", GraduationYear = 1800 };

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/candidate-profile", invalid)).StatusCode);

        client.DefaultRequestHeaders.Remove("X-Agentic-Request");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/candidate-profile", ProfileUpdate())).StatusCode);
    }

    private static async Task Register(HttpClient client, string email, string careerStage)
    {
        var registered = await client.PostAsJsonAsync("/api/account/register", new
        {
            displayName = "Profile Example",
            email,
            password = "Fictional profile passphrase 2026"
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var updated = await client.PutAsJsonAsync("/api/account/profile", new { displayName = "Profile Example", careerStage });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    }

    private static ProfileRequest ProfileUpdate() => new(
        "Program and operations leader",
        "Improves service delivery through cross-functional planning.",
        "Manager",
        "Program Manager",
        "Healthcare",
        "New York, Remote",
        "hybrid",
        "full_time",
        "Operations",
        "Authorized to work in the United States",
        true,
        "https://linkedin.com/in/example",
        "https://example.test/portfolio",
        "Fictional resume text",
        "Example University",
        "Bachelor of Arts",
        "Public Health",
        2020,
        6,
        "Community Health Network",
        "Operations Manager",
        [new("Operations", "Led a documented service improvement initiative.", "Verified", "Performance review")]);

    private sealed record ProfileRequest(
        string Headline,
        string ProfessionalSummary,
        string TargetLevel,
        string TargetRoleFamilies,
        string TargetIndustries,
        string PreferredLocations,
        string WorkModePreference,
        string EmploymentTypePreference,
        string Skills,
        string WorkAuthorization,
        bool RequiresSponsorship,
        string LinkedInUrl,
        string PortfolioUrl,
        string ResumeText,
        string Institution,
        string Degree,
        string FieldOfStudy,
        int? GraduationYear,
        int? YearsExperience,
        string RecentEmployer,
        string RecentJobTitle,
        IReadOnlyList<EvidenceRequest> Evidence);

    private sealed record EvidenceRequest(string Category, string Statement, string VerificationStatus, string Source);
}
