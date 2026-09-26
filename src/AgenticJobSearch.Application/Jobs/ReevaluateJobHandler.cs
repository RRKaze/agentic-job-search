using AgenticJobSearch.Application.Abstractions;

namespace AgenticJobSearch.Application.Jobs;

public sealed class ReevaluateJobHandler(
    IJobRepository jobs,
    ICandidateProfileRepository candidateProfiles,
    JobScoringService scoring)
{
    public async Task<JobEvaluationDto> HandleAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobs.GetOwnedAsync(jobId, cancellationToken)
            ?? throw new EvaluationFailure(404, "Job not found.");
        var profile = await candidateProfiles.GetDefaultAsync(cancellationToken);
        var evaluation = scoring.Evaluate(job, profile);

        await jobs.SaveEvaluationAsync(evaluation, cancellationToken);
        return evaluation.ToDto();
    }

    public async Task<IReadOnlyList<JobEvaluationDto>> HistoryAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var evaluations = await jobs.ListEvaluationsAsync(jobId, cancellationToken);
        return evaluations.Select(evaluation => evaluation.ToDto()).ToList();
    }
}
