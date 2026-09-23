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
