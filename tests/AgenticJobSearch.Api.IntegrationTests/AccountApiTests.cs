using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class AccountApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Password = "Fictional test passphrase 2026";
    private async Task Register(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/account/register", new { displayName = "Example Person", email, password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain("password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("httponly", StringComparison.OrdinalIgnoreCase) && value.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Account_choice_persists_across_logout_and_login()
    {
        using var client = fixture.CreateClient();
        await Register(client, "journey@example.test");
        var initial = await client.GetFromJsonAsync<JsonElement>("/api/account/me");
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("careerStage").ValueKind);
        foreach (var stage in new[] { "new_graduate", "experienced_worker" })
        {
            var saved = await client.PutAsJsonAsync("/api/account/profile", new { displayName = "Updated Name", careerStage = stage });
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/account/logout", new {})).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/account/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/jobs")).StatusCode);
        var bad = await client.PostAsJsonAsync("/api/account/login", new { email = "journey@example.test", password = "incorrect password" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        var login = await client.PostAsJsonAsync("/api/account/login", new { email = " JOURNEY@EXAMPLE.TEST ", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var profile = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("experienced_worker", profile.GetProperty("careerStage").GetString());
        Assert.Equal("Updated Name", profile.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Users_cannot_read_each_others_jobs_or_candidate_profiles()
    {
        using var first = fixture.CreateClient(); using var second = fixture.CreateClient();
        await Register(first, "first@example.test"); await Register(second, "second@example.test");
        var created = await first.PostAsJsonAsync("/api/jobs", new { title = "Private job", company = "Example", sourceText = "Build software and maintain backend services." });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Single((await first.GetFromJsonAsync<JsonElement[]>("/api/jobs"))!);
        Assert.Empty((await second.GetFromJsonAsync<JsonElement[]>("/api/jobs"))!);
        var firstProfile = await first.GetFromJsonAsync<JsonElement>("/api/candidate-profile");
        var secondProfile = await second.GetFromJsonAsync<JsonElement>("/api/candidate-profile");
        Assert.NotEqual(firstProfile.GetProperty("id").GetString(), secondProfile.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Validation_duplicate_email_and_missing_csrf_header_are_rejected()
    {
        using var client = fixture.CreateClient();
        var weak = await client.PostAsJsonAsync("/api/account/register", new { displayName = "Name", email = "not-email", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        await Register(client, "duplicate@example.test");
        var duplicate = await client.PostAsJsonAsync("/api/account/register", new { displayName = "Name", email = "DUPLICATE@example.test", password = Password });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var invalid = await client.PutAsJsonAsync("/api/account/profile", new { displayName = "Name", careerStage = "invented" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await client.GetFromJsonAsync<JsonElement>("/api/account/me")).GetProperty("careerStage").ValueKind);
        client.DefaultRequestHeaders.Remove("X-Agentic-Request");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/account/logout", new {})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/account/profile", new { displayName = "Name", careerStage = "new_graduate" })).StatusCode);
    }
}
