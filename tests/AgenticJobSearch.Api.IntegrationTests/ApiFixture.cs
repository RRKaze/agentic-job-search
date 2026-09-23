using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using AgenticJobSearch.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("agentic_job_search_tests")
        .WithUsername("agentic")
        .WithPassword("agentic")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-h", "127.0.0.1", "-U", "agentic"))
        .Build();

    private WebApplicationFactory<Program>? application;

    public TestSourceVerifier SourceVerifier { get; } = new();
    public const string ImportToken = "fictional-integration-test-token";
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        application = new TestApplicationFactory(database.GetConnectionString(), SourceVerifier);
        Client = CreateClient();
        var response = await Client.PostAsJsonAsync("/api/account/register", new { displayName = "Fixture Owner", email = "owner@example.test", password = "Fictional test passphrase 2026" });
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<JsonElement>();
        application.Services.GetRequiredService<IConfiguration>()["Accounts:LegacyWorkspaceOwnerId"] = account.GetProperty("id").GetString();
    }

    public HttpClient CreateClient()
    {
        var client = application!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Agentic-Request", "1");
        return client;
    }

    public void SetImportsEnabled(bool enabled) =>
        application!.Services.GetRequiredService<IConfiguration>()["Imports:Enabled"] = enabled.ToString();

    public async Task RejectApplicationWritesAsync()
    {
        await using var scope = application!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION reject_test_application() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'fictional test write failure'; END $$;
            CREATE TRIGGER reject_test_application BEFORE INSERT ON "Applications"
            FOR EACH ROW EXECUTE FUNCTION reject_test_application();
            """);
    }

    public async Task SeedTrackedApplicationAsync()
    {
        await using var scope = application!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
        var job = new AgenticJobSearch.Domain.Job
        {
            Id = Guid.NewGuid(),
            ExternalId = "JOB-DASHBOARD",
            SourceRepository = "RRKaze/job-search",
            Title = "Platform Engineer",
            Company = "Fictional Systems",
            Location = "Remote",
            SourceUrl = "https://example.com/jobs/platform",
            TrackingStage = "interviewing",
            Priority = "high",
            FitRationale = "Matches the fictional candidate's backend experience.",
            GapsNotes = "Clarify the on-call rotation.",
            StatusDate = new DateOnly(2026, 9, 21),
            VerifiedDate = new DateOnly(2026, 9, 20)
        };
        job.Application = new AgenticJobSearch.Domain.Application
        {
            Id = Guid.NewGuid(),
            ExternalId = "APP-DASHBOARD",
            SourceRepository = "RRKaze/job-search",
            JobId = job.Id,
            State = AgenticJobSearch.Domain.ApplicationState.Interview,
            SubmittedDate = new DateOnly(2026, 9, 18),
            ResumeVersion = "platform-v1",
            NextFollowUp = new DateOnly(2026, 9, 25),
            Notes = "Fictional recruiter screen scheduled."
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
    }

    public async Task AssertImportedStateAsync()
    {
        await using var scope = application!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
        var job = await db.Jobs.SingleAsync();
        var app = await db.Applications.SingleAsync();
        Assert.Equal("rejected", job.TrackingStage);
        Assert.Equal(AgenticJobSearch.Domain.ApplicationState.Rejected, app.State);
        Assert.Equal(new DateOnly(2026, 9, 21), app.SubmittedDate);
        Assert.Null(app.SubmittedAt);
        Assert.Null(app.NextFollowUp);
        Assert.Equal("First line\nSecond line", app.Notes);
        Assert.Equal(5, await db.TrackingChanges.CountAsync());
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (application is not null)
        {
            await application.DisposeAsync();
        }

        await database.DisposeAsync();
    }

    private sealed class TestApplicationFactory(string connectionString, TestSourceVerifier verifier) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Imports:Token"] = ImportToken,
                ["Imports:Enabled"] = "true"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AgenticJobSearch.Application.Imports.ISourceHeadVerifier>();
                services.AddSingleton<AgenticJobSearch.Application.Imports.ISourceHeadVerifier>(verifier);
                services.RemoveAll<DbContextOptions<JobSearchDbContext>>();
                services.RemoveAll<JobSearchDbContext>();
                services.AddDbContext<JobSearchDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}

public sealed class TestSourceVerifier : AgenticJobSearch.Application.Imports.ISourceHeadVerifier
{
    public string Head { get; set; } = new('1', 40);
    public Task<string> GetHeadAsync(CancellationToken cancellationToken) => Task.FromResult(Head);
}
