using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgenticJobSearch.Application.Imports;
namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class ImportApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    const string Endpoint = "/api/v1/imports/job-records";
    static Dictionary<string, string?> Job() => new()
    {
        ["job_id"] = "JOB-1",
        ["company"] = "Fictional Example",
        ["role"] = "Engineer",
        ["location"] = "Remote",
        ["salary_text"] = null,
        ["priority"] = "high",
        ["fit_rationale"] = "Fictional fit",
        ["gaps_notes"] = null,
        ["stage"] = "lead",
        ["status_date"] = null,
        ["source_url"] = "https://example.com/jobs/1",
        ["verified_date"] = "2026-09-21"
    };
    ImportRequest Request(bool dryRun = true) => new("1.0", dryRun, new("RRKaze/job-search", "main", fixture.SourceVerifier.Head, null), [Job()], []);
    async Task<JsonElement> Send(ImportRequest request, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var response = await fixture.Client.PostAsJsonAsync(Endpoint, request, ImportJson.Options);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}; got {response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    [Fact]
    public async Task Import_is_retired_when_the_database_owns_workflow_status()
    {
        fixture.SetImportsEnabled(false);
        try
        {
            fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.ImportToken);
            using var response = await fixture.Client.PostAsJsonAsync(Endpoint, Request(), ImportJson.Options);
            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("IMPORT_RETIRED", body.GetProperty("error").GetProperty("code").GetString());
        }
        finally
        {
            fixture.SetImportsEnabled(true);
        }
    }
    [Fact]
    public async Task Dry_run_leaves_checkpoint_and_jobs_untouched()
    {
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.ImportToken);
        var response = await Send(Request());
        Assert.Equal(1, response.GetProperty("counts").GetProperty("jobs").GetProperty("inserted").GetInt32());
        var checkpoint = await fixture.Client.GetFromJsonAsync<JsonElement>(Endpoint + "/checkpoint");
        Assert.Equal(JsonValueKind.Null, checkpoint.GetProperty("source_commit").ValueKind);
        var jobs = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/jobs");
        Assert.Empty(jobs.EnumerateArray());
    }

    [Fact]
    public async Task Discovery_application_rejection_and_retries_preserve_one_record_and_history()
    {
        // This test has its own database because it changes the import checkpoint.
        await using var isolated = new OwnedFixture();
        await isolated.Value.InitializeAsync();
        var f = isolated.Value;
        f.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.ImportToken);
        async Task<JsonElement> Post(ImportRequest r, HttpStatusCode expected = HttpStatusCode.OK)
        {
            using var response = await f.Client.PostAsJsonAsync(Endpoint, r, ImportJson.Options);
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {text}");
            return JsonDocument.Parse(text).RootElement.Clone();
        }
        var first = new ImportRequest("1.0", false, new("RRKaze/job-search", "main", f.SourceVerifier.Head, null), [Job()], []);
        Assert.Equal("imported", (await Post(first)).GetProperty("result").GetString());
        Assert.Equal("already_imported", (await Post(first)).GetProperty("result").GetString());
        var second = first with { Source = first.Source with { CommitSha = new('2', 40), ExpectedPreviousCommit = first.Source.CommitSha } };
        f.SourceVerifier.Head = second.Source.CommitSha;
        second.Jobs[0]["stage"] = "applied";
        second.Applications.Add(new() { ["application_id"] = "APP-1", ["job_id"] = "JOB-1", ["submitted_date"] = "2026-09-21", ["status"] = "submitted", ["resume_version"] = "master-v1", ["referral_contact"] = null, ["next_follow_up"] = "2026-09-28", ["outcome"] = null, ["notes"] = "First line\nSecond line" });
        Assert.Equal(1, (await Post(second)).GetProperty("counts").GetProperty("applications").GetProperty("inserted").GetInt32());
        var third = second with { Source = second.Source with { CommitSha = new('3', 40), ExpectedPreviousCommit = second.Source.CommitSha } };
        f.SourceVerifier.Head = third.Source.CommitSha;
        third.Jobs[0]["stage"] = "rejected"; third.Applications[0]["status"] = "rejected"; third.Applications[0]["outcome"] = "rejected";
        // A closed application with a follow-up is invalid, with no partial job update.
        await Post(third, HttpStatusCode.UnprocessableEntity);
        var check = await f.Client.GetFromJsonAsync<JsonElement>(Endpoint + "/checkpoint");
        Assert.Equal(second.Source.CommitSha, check.GetProperty("source_commit").GetString());
        third.Applications[0]["next_follow_up"] = null;
        await Post(third);
        Assert.Equal("already_imported", (await Post(third)).GetProperty("result").GetString());
        await f.AssertImportedStateAsync();
        // Same commit, altered content cannot masquerade as a retry.
        third.Jobs[0]["priority"] = "low";
        Assert.Equal("SOURCE_CONTENT_CONFLICT", (await Post(third, HttpStatusCode.Conflict)).GetProperty("error").GetProperty("code").GetString());
        await Post(second, HttpStatusCode.Conflict);
        f.SourceVerifier.Head = new('4', 40);
        var fourth = third with { Source = third.Source with { CommitSha = f.SourceVerifier.Head, ExpectedPreviousCommit = first.Source.CommitSha } };
        Assert.Equal("CHECKPOINT_CONFLICT", (await Post(fourth, HttpStatusCode.Conflict)).GetProperty("error").GetProperty("code").GetString());
        fourth = fourth with { Source = fourth.Source with { ExpectedPreviousCommit = third.Source.CommitSha }, Jobs = [], Applications = [] };
        Assert.Equal("SOURCE_RECORD_MISSING", (await Post(fourth, HttpStatusCode.Conflict)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Authentication_and_strict_request_validation_fail_safely()
    {
        using var unauthorized = new HttpRequestMessage(HttpMethod.Get, Endpoint + "/checkpoint");
        unauthorized.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "incorrect");
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.SendAsync(unauthorized)).StatusCode);
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.ImportToken);
        var missingDryRun = JsonSerializer.Serialize(Request(), ImportJson.Options).Replace("\"dry_run\":true,", "");
        using var invalid = await fixture.Client.PostAsync(Endpoint, new StringContent(missingDryRun, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var request = Request(); request.Jobs[0]["verified_date"] = "2026-02-30";
        await Send(request, HttpStatusCode.UnprocessableEntity);
        request = Request(); request.Jobs[0]["extra"] = "not allowed";
        await Send(request, HttpStatusCode.UnprocessableEntity);
        request = Request() with { Source = Request().Source with { Repository = "other/repository" } };
        await Send(request, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Concurrent_different_payloads_for_one_commit_have_only_one_winner()
    {
        await using var isolated = new OwnedFixture(); await isolated.Value.InitializeAsync();
        var f = isolated.Value;
        f.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.ImportToken);
        var first = new ImportRequest("1.0", false, new("RRKaze/job-search", "main", f.SourceVerifier.Head, null), [Job()], []);
        var second = first with { Jobs = [Job()] }; second.Jobs[0]["priority"] = "low";
        var responses = await Task.WhenAll(f.Client.PostAsJsonAsync(Endpoint, first, ImportJson.Options), f.Client.PostAsJsonAsync(Endpoint, second, ImportJson.Options));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in responses) response.Dispose();
        var jobs = await f.Client.GetFromJsonAsync<JsonElement>("/api/jobs");
        Assert.Single(jobs.EnumerateArray());
    }


    [Fact]
    public async Task Database_failure_rolls_back_jobs_applications_and_checkpoint_together()
    {
        await using var isolated = new OwnedFixture(); await isolated.Value.InitializeAsync();
        var f = isolated.Value;
        await f.RejectApplicationWritesAsync();
        f.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.ImportToken);
        var request = new ImportRequest("1.0", false, new("RRKaze/job-search", "main", f.SourceVerifier.Head, null), [Job()], []);
        request.Jobs[0]["stage"] = "applied";
        request.Applications.Add(new() { ["application_id"] = "APP-1", ["job_id"] = "JOB-1", ["submitted_date"] = "2026-09-21", ["status"] = "submitted", ["resume_version"] = null, ["referral_contact"] = null, ["next_follow_up"] = null, ["outcome"] = null, ["notes"] = null });
        using var response = await f.Client.PostAsJsonAsync(Endpoint, request, ImportJson.Options);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var jobs = await f.Client.GetFromJsonAsync<JsonElement>("/api/jobs"); Assert.Empty(jobs.EnumerateArray());
        var checkpoint = await f.Client.GetFromJsonAsync<JsonElement>(Endpoint + "/checkpoint");
        Assert.Equal(JsonValueKind.Null, checkpoint.GetProperty("source_commit").ValueKind);
    }
    private sealed class OwnedFixture : IAsyncDisposable
    {
        public ApiFixture Value { get; } = new();
        public async ValueTask DisposeAsync() => await Value.DisposeAsync();
    }
}
