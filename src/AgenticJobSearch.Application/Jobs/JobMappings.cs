using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public static class JobMappings
{
    public static JobDto ToDto(this Job job)
    {
        var evaluation = job.Evaluation;

        return new JobDto(
            job.Id,
            job.Title,
            job.Company,
            job.Location,
            job.Seniority,
            job.WorkMode,
            job.LifecycleState,
            evaluation?.Eligibility ?? EligibilityDecision.NeedsReview,
            evaluation?.FitScore ?? 0,
            evaluation?.ApplicationPriority ?? 0,
            evaluation?.Recommendation ?? "Not evaluated",
            evaluation?.Explanation ?? string.Empty,
            evaluation?.Factors
                .OrderByDescending(factor => Math.Abs(factor.ScoreImpact))
                .ThenBy(factor => factor.Name)
                .Select(factor => new JobEvaluationFactorDto(
                    factor.Name,
                    factor.Weight,
                    factor.ScoreImpact,
                    factor.Rationale))
                .ToList() ?? [],
            job.CreatedAt);
    }
}
