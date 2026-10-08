using Microsoft.Extensions.Configuration;
using AgenticJobSearch.Application.Accounts;
using AgenticJobSearch.Infrastructure.Accounts;
using Microsoft.Extensions.Hosting;
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

    public TestRecoverySender RecoverySender { get; } = new();
    public TestClock Clock { get; } = new();
    private static int clientNumber;
    public TestSourceVerifier SourceVerifier { get; } = new();
    public const string ImportToken = "fictional-integration-test-token";
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        application = new TestApplicationFactory(database.GetConnectionString(), SourceVerifier, RecoverySender, Clock);
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
            await db.Database.MigrateAsync();
        }
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
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.0.2.{Interlocked.Increment(ref clientNumber) % 250 + 1}");
        return client;
    }

    public async Task WithDbAsync(Func<JobSearchDbContext, Task> action)
    {
        await using var scope = application!.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<JobSearchDbContext>());
    }

    public async Task DrainRecoveryAsync()
    {
        for (var i = 0; i < 100; i++)
        {
            await using var scope = application!.Services.CreateAsyncScope();
            if (!await scope.ServiceProvider.GetRequiredService<RecoveryEmailProcessor>().ProcessNextAsync(default)) return;
        }
        throw new InvalidOperationException("Recovery queue did not drain.");
    }

    public void SetRecoveryEnabled(bool enabled) => application!.Services
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<PasswordRecoveryOptions>>().Value.Enabled = enabled;

    public void SetImportsEnabled(bool enabled) =>
        application!.Services.GetRequiredService<IConfiguration>()["Imports:Enabled"] = enabled.ToString();

    public void SetRegistration(bool enabled, string bootstrapToken = "")
    {
        var configuration = application!.Services.GetRequiredService<IConfiguration>();
        configuration["Accounts:RegistrationEnabled"] = enabled.ToString();
        configuration["Accounts:RegistrationBootstrapToken"] = bootstrapToken;
    }

    public async Task<int> CountDataProtectionKeysAsync()
    {
        await using var scope = application!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"DataProtectionKeys\"";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

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

    private sealed class TestApplicationFactory(string connectionString, TestSourceVerifier verifier, TestRecoverySender sender, TestClock clock) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:JobSearch"] = connectionString,
                ["Imports:Token"] = ImportToken,
                ["Imports:Enabled"] = "true",
                ["PasswordRecovery:Enabled"] = "true",
                ["PasswordRecovery:PublicBaseUrl"] = "https://app.example.test",
                ["PasswordRecovery:From"] = "Test <accounts@example.test>",
                ["PasswordRecovery:ResendApiKey"] = "fictional-test-key"
            }));
            builder.ConfigureServices(services =>
            {
                foreach (var service in services.Where(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(RecoveryEmailWorker)).ToArray()) services.Remove(service);
                services.RemoveAll<IRecoveryEmailSender>();
                services.AddSingleton<IRecoveryEmailSender>(sender);
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
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

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan amount) => now += amount;
}

public sealed class TestRecoverySender : IRecoveryEmailSender
{
    public List<(Guid Id, RecoveryEmailMessage Message)> Attempts { get; } = [];
    public bool Fail { get; set; }
    public Task SendAsync(Guid id, RecoveryEmailMessage message, CancellationToken cancellationToken)
    {
        Attempts.Add((id, message));
        if (Fail) throw new HttpRequestException("Fictional provider failure");
        return Task.CompletedTask;
    }
}
