using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed class UpdateJobWorkflowHandler(
    IJobRepository jobs,
    ICurrentUser currentUser,
    JobWorkflowPolicy policy)
{
    public async Task<JobDto> HandleAsync(Guid id, UpdateJobWorkflowRequest request, CancellationToken cancellationToken)
    {
        var job = await jobs.GetOwnedAsync(id, cancellationToken) ?? throw new WorkflowFailure(404, "Job not found.");
        var previous = policy.CurrentStatus(job);
        var next = policy.ValidateTransition(job, request);
        var now = DateTimeOffset.UtcNow;
        var statusDate = request.StatusDate!.Value;
        var applicationCreated = job.Application is null;

        job.Application ??= new Domain.Application
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            CreatedAt = now
        };

        job.TrackingStage = next;
        job.StatusDate = statusDate;
        job.UpdatedAt = now;
        job.LifecycleState = JobLifecycleState.Applied;

        var application = job.Application;
        application.State = policy.ApplicationStateFor(next);
        application.SubmittedDate = request.SubmittedDate ?? application.SubmittedDate ?? statusDate;
        application.ResumeVersion = Clean(request.ResumeVersion);
        application.NextFollowUp = request.NextFollowUp;
        application.Outcome = Clean(request.Outcome);
        application.Notes = Clean(request.Notes);

        var change = previous == next ? null : new WorkflowChange
        {
            Id = Guid.NewGuid(),
            OwnerId = currentUser.Id!.Value,
            JobId = job.Id,
            PreviousStatus = previous,
            CurrentStatus = next,
            ChangedAt = now
        };

        await jobs.SaveWorkflowChangeAsync(job, change, applicationCreated, cancellationToken);
        return job.ToDto();
    }

    public async Task<IReadOnlyList<WorkflowChangeDto>> HistoryAsync(Guid id, CancellationToken cancellationToken) =>
        (await jobs.ListWorkflowChangesAsync(id, cancellationToken))
            .Select(change => new WorkflowChangeDto(change.PreviousStatus, change.CurrentStatus, change.ChangedAt))
            .ToList();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
