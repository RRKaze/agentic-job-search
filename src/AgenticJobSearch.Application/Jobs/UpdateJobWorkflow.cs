using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed record UpdateJobWorkflowRequest(
    string? Status,
    DateOnly? StatusDate,
    DateOnly? SubmittedDate,
    string? ResumeVersion,
    DateOnly? NextFollowUp,
    string? Outcome,
    string? Notes);

public sealed record WorkflowChangeDto(string PreviousStatus, string CurrentStatus, DateTimeOffset ChangedAt);

public sealed class WorkflowFailure(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class UpdateJobWorkflowHandler(IJobRepository jobs, ICurrentUser currentUser)
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["lead"] = ["applied"],
        ["applied"] = ["interviewing", "rejected", "withdrawn"],
        ["interviewing"] = ["applied", "offer", "rejected", "withdrawn"],
        ["offer"] = ["interviewing", "rejected", "withdrawn"],
        ["rejected"] = ["applied", "interviewing"],
        ["withdrawn"] = ["applied"]
    };

    public async Task<JobDto> HandleAsync(Guid id, UpdateJobWorkflowRequest request, CancellationToken cancellationToken)
    {
        var job = await jobs.GetOwnedAsync(id, cancellationToken) ?? throw new WorkflowFailure(404, "Job not found.");
        var next = request.Status?.Trim().ToLowerInvariant() ?? "";
        var previous = CanonicalStatus(job);
        Validate(previous, next, request);

        var now = DateTimeOffset.UtcNow;
        var statusDate = request.StatusDate!.Value;
        var applicationCreated = job.Application is null;
        if (applicationCreated)
        {
            job.Application = new Domain.Application
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                State = ApplicationState.Submitted,
                SubmittedDate = request.SubmittedDate ?? statusDate,
                CreatedAt = now
            };
        }

        job.TrackingStage = next;
        job.StatusDate = statusDate;
        job.UpdatedAt = now;
        job.LifecycleState = JobLifecycleState.Applied;
        var application = job.Application!;
        application.State = next switch
        {
            "applied" => ApplicationState.Submitted,
            "interviewing" => ApplicationState.Interview,
            "offer" => ApplicationState.Offer,
            "rejected" => ApplicationState.Rejected,
            _ => ApplicationState.Withdrawn
        };
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

    private static void Validate(string previous, string next, UpdateJobWorkflowRequest request)
    {
        if (request.StatusDate is null) throw new WorkflowFailure(400, "Status date is required.");
        if (!AllowedTransitions.ContainsKey(previous)) throw new WorkflowFailure(409, "This job status cannot be changed in the application.");
        if (next != previous && !AllowedTransitions[previous].Contains(next, StringComparer.Ordinal))
            throw new WorkflowFailure(409, $"Status cannot move from {previous} to {next}.");
        if (next == "lead") throw new WorkflowFailure(409, "An application cannot be moved back to Saved.");
        if (next is "rejected" or "withdrawn" && request.NextFollowUp is not null)
            throw new WorkflowFailure(400, "Rejected and withdrawn applications cannot have a follow-up date.");
        ValidateLength(request.ResumeVersion, 2000, "Resume version");
        ValidateLength(request.Outcome, 2000, "Outcome");
        ValidateLength(request.Notes, 20000, "Notes");
    }

    private static string CanonicalStatus(Job job)
    {
        if (!string.IsNullOrWhiteSpace(job.TrackingStage)) return job.TrackingStage.ToLowerInvariant();
        if (job.Application is null) return "lead";
        return job.Application.State switch
        {
            ApplicationState.Submitted => "applied",
            ApplicationState.Screen or ApplicationState.Interview => "interviewing",
            ApplicationState.Offer => "offer",
            ApplicationState.Rejected => "rejected",
            ApplicationState.Withdrawn => "withdrawn",
            _ => "lead"
        };
    }

    private static void ValidateLength(string? value, int maximum, string field)
    {
        if (value?.Length > maximum) throw new WorkflowFailure(400, $"{field} is too long.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
