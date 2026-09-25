using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed class JobWorkflowPolicy
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["lead"] = ["applied"],
            ["applied"] = ["interviewing", "rejected", "withdrawn"],
            ["interviewing"] = ["applied", "offer", "rejected", "withdrawn"],
            ["offer"] = ["interviewing", "rejected", "withdrawn"],
            ["rejected"] = ["applied", "interviewing"],
            ["withdrawn"] = ["applied"]
        };

    public string CurrentStatus(Job job)
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

    public string ValidateTransition(Job job, UpdateJobWorkflowRequest request)
    {
        var previous = CurrentStatus(job);
        var next = request.Status?.Trim().ToLowerInvariant() ?? "";

        if (request.StatusDate is null) throw new WorkflowFailure(400, "Status date is required.");
        if (!AllowedTransitions.TryGetValue(previous, out var allowed))
            throw new WorkflowFailure(409, "This job status cannot be changed in the application.");
        if (next == "lead") throw new WorkflowFailure(409, "An application cannot be moved back to Saved.");
        if (next != previous && !allowed.Contains(next, StringComparer.Ordinal))
            throw new WorkflowFailure(409, $"Status cannot move from {previous} to {next}.");
        if (next is "rejected" or "withdrawn" && request.NextFollowUp is not null)
            throw new WorkflowFailure(400, "Rejected and withdrawn applications cannot have a follow-up date.");

        ValidateLength(request.ResumeVersion, 2000, "Resume version");
        ValidateLength(request.Outcome, 2000, "Outcome");
        ValidateLength(request.Notes, 20000, "Notes");
        return next;
    }

    public ApplicationState ApplicationStateFor(string status) => status switch
    {
        "applied" => ApplicationState.Submitted,
        "interviewing" => ApplicationState.Interview,
        "offer" => ApplicationState.Offer,
        "rejected" => ApplicationState.Rejected,
        "withdrawn" => ApplicationState.Withdrawn,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown workflow status.")
    };

    private static void ValidateLength(string? value, int maximum, string field)
    {
        if (value?.Length > maximum) throw new WorkflowFailure(400, $"{field} is too long.");
    }
}
