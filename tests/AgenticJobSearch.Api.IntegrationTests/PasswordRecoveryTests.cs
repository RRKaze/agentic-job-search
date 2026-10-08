using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticJobSearch.Domain;
using AgenticJobSearch.Infrastructure.Accounts;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class PasswordRecoveryTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string OldPassword = "Fictional original passphrase 2026";
    private const string NewPassword = "Fictional replacement passphrase 2026";
    private async Task Register(HttpClient client, string email) =>
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/account/register",
            new { email, displayName = "Fictional Candidate", password = OldPassword })).StatusCode);
    private static Task<HttpResponseMessage> Request(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/account/forgot-password", new { email });
    private static Task<HttpResponseMessage> Reset(HttpClient client, string token, string confirmPassword = NewPassword) =>
        client.PostAsJsonAsync("/api/account/reset-password", new { token, password = NewPassword, confirmPassword });
    private string Token(string email) => Regex.Match(fixture.RecoverySender.Attempts.Last(x => x.Message.To == email && x.Message.Subject.StartsWith("Reset")).Message.Text,
        "#token=([A-F0-9]{64})").Groups[1].Value;

    [Fact]
    public async Task Reset_preserves_account_and_jobs_revokes_sessions_and_sends_notice()
    {
        using var owner = fixture.CreateClient(); using var resetClient = fixture.CreateClient(); using var other = fixture.CreateClient();
        const string email = "recover-owner@example.test";
        await Register(owner, email); await Register(other, "recover-other@example.test");
        var before = await owner.GetFromJsonAsync<JsonElement>("/api/account/me");
        await owner.PutAsJsonAsync("/api/account/profile", new { displayName = "Saved Name", careerStage = "experienced_worker" });
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync("/api/jobs", new { title = "Saved job", company = "Fictional", sourceText = "Maintain backend software services." })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await Request(resetClient, " RECOVER-OWNER@example.test ")).StatusCode);
        await fixture.DrainRecoveryAsync();
        var token = Token(email);
        Assert.Equal(64, token.Length);
        await fixture.WithDbAsync(async db => {
            var stored = await db.PasswordResetTokens.SingleAsync(x => x.TokenHash == PasswordRecoveryService.HashToken(token));
            Assert.NotEqual(token, stored.TokenHash);
            Assert.All(await db.RecoveryEmails.Where(x => x.Email == email).ToListAsync(), x => Assert.Null(x.ProtectedMessage));
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Reset(resetClient, token, "Mismatch password 2026")).StatusCode);
        var result = await Reset(resetClient, token);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(result.Headers.CacheControl?.NoStore);
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.GetAsync("/api/account/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/account/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.PostAsJsonAsync("/api/account/login", new { email, password = OldPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync("/api/account/login", new { email, password = NewPassword })).StatusCode);
        var after = await owner.GetFromJsonAsync<JsonElement>("/api/account/me");
        Assert.Equal(before.GetProperty("id").GetString(), after.GetProperty("id").GetString());
        Assert.Equal("Saved Name", after.GetProperty("displayName").GetString());
        Assert.Equal("experienced_worker", after.GetProperty("careerStage").GetString());
        Assert.Single((await owner.GetFromJsonAsync<JsonElement[]>("/api/jobs"))!);
        Assert.Equal(HttpStatusCode.BadRequest, (await Reset(resetClient, token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await resetClient.GetAsync("/api/account/me")).StatusCode);
        await fixture.DrainRecoveryAsync();
        Assert.Contains(fixture.RecoverySender.Attempts, x => x.Message.To == email && x.Message.Subject.Contains("was changed"));
    }

    [Fact]
    public async Task Requests_are_uniform_throttled_and_do_not_require_registration()
    {
        using var client = fixture.CreateClient();
        const string email = "uniform@example.test";
        await Register(client, email);
        fixture.SetRegistration(false);
        try {
            var known = await Request(client, email);
            var unknown = await Request(client, "unknown@example.test");
            var invalid = await Request(client, "invalid");
            Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
            Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
            Assert.Equal(await known.Content.ReadAsStringAsync(), await invalid.Content.ReadAsStringAsync());
            await Request(client, email);
            await fixture.DrainRecoveryAsync();
            Assert.Single(fixture.RecoverySender.Attempts, x => x.Message.To == email);
            Assert.DoesNotContain(fixture.RecoverySender.Attempts, x => x.Message.To == "unknown@example.test");
            for (var i = 0; i < 2; i++) { fixture.Clock.Advance(TimeSpan.FromMinutes(2)); await Request(client, email); }
            fixture.Clock.Advance(TimeSpan.FromMinutes(2)); await Request(client, email);
            await fixture.WithDbAsync(async db => Assert.Equal(3, await db.RecoveryEmails.CountAsync(x => x.Email == email)));
        } finally { fixture.SetRegistration(true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_resets_allow_only_one_winner_and_cancel_queued_requests(bool differentLinks)
    {
        using var client = fixture.CreateClient();
        var email = $"race-{differentLinks}@example.test".ToLowerInvariant();
        await Register(client, email); await Request(client, email); await fixture.DrainRecoveryAsync();
        var first = Token(email); var second = first;
        if (differentLinks) { fixture.Clock.Advance(TimeSpan.FromMinutes(2)); await Request(client, email); await fixture.DrainRecoveryAsync(); second = Token(email); }
        fixture.Clock.Advance(TimeSpan.FromMinutes(2)); await Request(client, email);
        using var a = fixture.CreateClient(); using var b = fixture.CreateClient();
        var results = await Task.WhenAll(Reset(a, first), Reset(b, second));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.BadRequest);
        var sends = fixture.RecoverySender.Attempts.Count(x => x.Message.To == email && x.Message.Subject.StartsWith("Reset"));
        await fixture.DrainRecoveryAsync();
        Assert.Equal(sends, fixture.RecoverySender.Attempts.Count(x => x.Message.To == email && x.Message.Subject.StartsWith("Reset")));
    }

    [Fact]
    public async Task Failed_delivery_retries_identical_encrypted_message_across_scopes_then_erases_payload()
    {
        using var client = fixture.CreateClient(); const string email = "retry@example.test";
        await Register(client, email); await Request(client, email);
        fixture.RecoverySender.Fail = true;
        try { await fixture.DrainRecoveryAsync(); } finally { fixture.RecoverySender.Fail = false; }
        var first = fixture.RecoverySender.Attempts.Last(x => x.Message.To == email);
        var token = Token(email);
        await fixture.WithDbAsync(async db => {
            var pending = await db.RecoveryEmails.SingleAsync(x => x.Email == email);
            Assert.Null(pending.CompletedAt); Assert.NotNull(pending.ProtectedMessage);
            Assert.DoesNotContain(token, pending.ProtectedMessage);
            Assert.DoesNotContain(email, pending.ProtectedMessage);
        });
        fixture.Clock.Advance(TimeSpan.FromSeconds(31)); await fixture.DrainRecoveryAsync();
        Assert.Equal(first, fixture.RecoverySender.Attempts.Last(x => x.Message.To == email));
        await fixture.WithDbAsync(async db => { var sent = await db.RecoveryEmails.SingleAsync(x => x.Email == email); Assert.NotNull(sent.CompletedAt); Assert.Null(sent.ProtectedMessage); Assert.Equal(2, sent.Attempts); });
        fixture.Clock.Advance(TimeSpan.FromMinutes(31));
        Assert.Equal(HttpStatusCode.BadRequest, (await Reset(client, token)).StatusCode);
    }

    [Fact]
    public async Task Invalid_links_disabled_feature_and_missing_request_header_are_rejected()
    {
        using var client = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await Reset(client, "malformed")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Reset(client, new string('A', 64))).StatusCode);
        fixture.SetRecoveryEnabled(false);
        try { Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Request(client, "disabled@example.test")).StatusCode); }
        finally { fixture.SetRecoveryEnabled(true); }
        client.DefaultRequestHeaders.Remove("X-Agentic-Request");
        Assert.Equal(HttpStatusCode.Forbidden, (await Request(client, "csrf@example.test")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Reset(client, new string('A', 64))).StatusCode);
    }

    [Fact]
    public async Task Ip_request_limit_rejects_eleventh_attempt()
    {
        using var client = fixture.CreateClient();
        for (var i = 0; i < 10; i++) Assert.Equal(HttpStatusCode.Accepted, (await Request(client, "limited@example.test")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Request(client, "limited@example.test")).StatusCode);
    }
}
