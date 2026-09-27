using AgenticJobSearch.Application.Accounts;
using AgenticJobSearch.Application.Candidates;
using AgenticJobSearch.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticJobSearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<LoginAccountHandler>();
        services.AddScoped<GetAccountProfileHandler>();
        services.AddScoped<UpdateAccountProfileHandler>();
        services.AddScoped<GetCandidateProfileHandler>();
        services.AddScoped<UpdateCandidateProfileHandler>();
        services.AddScoped<AddJobHandler>();
        services.AddScoped<ReevaluateJobHandler>();
        services.AddScoped<UpdateJobWorkflowHandler>();
        services.AddSingleton<JobWorkflowPolicy>();
        services.AddScoped<JobNormalizer>();
        services.AddScoped<JobScoringService>();

        return services;
    }
}
