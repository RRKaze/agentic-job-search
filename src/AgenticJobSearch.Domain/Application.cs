namespace AgenticJobSearch.Domain;

public sealed class Application
{
    public string? SourceRepository { get; set; }
    public string? ExternalId { get; set; }
    public string? ImportedRecord { get; set; }
    public DateOnly? SubmittedDate { get; set; }
    public string? ResumeVersion { get; set; }
    public string? ReferralContact { get; set; }
    public DateOnly? NextFollowUp { get; set; }
    public string? Outcome { get; set; }
    public string? Notes { get; set; }
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job? Job { get; set; }
    public ApplicationState State { get; set; } = ApplicationState.NotStarted;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public List<Feedback> Feedback { get; set; } = [];
}
