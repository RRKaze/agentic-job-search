using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed class AddJobHandler(
    ICandidateProfileRepository candidateProfiles,
    IJobRepository jobs,
    JobNormalizer normalizer,
    JobScoringService scoring)
{
    public async Task<JobDto> HandleAsync(AddJobRequest request, CancellationToken cancellationToken)
    {
        var candidateProfile = await candidateProfiles.GetDefaultAsync(cancellationToken);
        var job = normalizer.Normalize(request);
        var evaluation = scoring.Evaluate(job, candidateProfile);

        job.Evaluation = evaluation;
        job.LifecycleState = evaluation.Eligibility == EligibilityDecision.Ineligible
            ? JobLifecycleState.Rejected
            : JobLifecycleState.Analyzed;
        job.UpdatedAt = DateTimeOffset.UtcNow;

        var saved = await jobs.AddAsync(job, cancellationToken);
        return saved.ToDto();
    }
}
