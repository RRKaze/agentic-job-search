using AgenticJobSearch.Application.Candidates;
using AgenticJobSearch.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticJobSearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetCandidateProfileHandler>();
        services.AddScoped<AddJobHandler>();
        services.AddScoped<JobNormalizer>();
        services.AddScoped<JobScoringService>();

        return services;
    }
}
