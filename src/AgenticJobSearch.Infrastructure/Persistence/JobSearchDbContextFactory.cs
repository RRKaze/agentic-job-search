using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class JobSearchDbContextFactory : IDesignTimeDbContextFactory<JobSearchDbContext>
{
    public JobSearchDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__JobSearch")
            ?? "Host=localhost;Port=5432;Database=agentic_job_search;Username=agentic;Password=agentic";
        var options = new DbContextOptionsBuilder<JobSearchDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new JobSearchDbContext(options);
    }
}
