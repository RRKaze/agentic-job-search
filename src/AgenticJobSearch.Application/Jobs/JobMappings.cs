using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public static class JobMappings
{
    public static JobDto ToDto(this Job job)
    {
        var evaluation = job.Evaluation;

        return new JobDto(
            job.Id,
            job.ExternalId,
            job.Title,
            job.Company,
            job.Location,
            job.SourceUrl,
            job.TrackingStage,
            job.SalaryText,
            job.Priority,
            job.FitRationale,
            job.GapsNotes,
            job.StatusDate,
            job.VerifiedDate,
            job.Application is null
                ? null
                : new ApplicationSummaryDto(
                    job.Application.ExternalId,
                    job.Application.State,
                    job.Application.SubmittedDate,
                    job.Application.ResumeVersion,
                    job.Application.NextFollowUp,
                    job.Application.Outcome,
                    job.Application.Notes),
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
                    factor.Rationale,
                    factor.Evidence.Select(item => new JobEvaluationEvidenceDto(
                        item.SourceEvidenceId,
                        item.Category,
                        item.Statement,
                        item.Source)).ToList()))
                .ToList() ?? [],
            job.CreatedAt);
    }
}
