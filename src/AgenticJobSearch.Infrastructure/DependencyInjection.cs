using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticJobSearch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("JobSearch")
            ?? "Host=localhost;Port=5432;Database=agentic_job_search;Username=agentic;Password=agentic";

        services.AddDbContext<JobSearchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
        services.AddScoped<IJobRepository, JobRepository>();

        return services;
    }
}
