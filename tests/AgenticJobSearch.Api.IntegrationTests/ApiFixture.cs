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
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-U", "agentic"))
        .Build();

    private WebApplicationFactory<Program>? application;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        application = new TestApplicationFactory(database.GetConnectionString());
        Client = application.CreateClient();
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

    private sealed class TestApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<JobSearchDbContext>>();
                services.RemoveAll<JobSearchDbContext>();
                services.AddDbContext<JobSearchDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
