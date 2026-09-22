namespace AgenticJobSearch.Domain;

public sealed class Job
{
    public Guid? OwnerId { get; set; }
    public string? SourceRepository { get; set; }
    public string? ExternalId { get; set; }
    public string? TrackingStage { get; set; }
    public string? ImportedRecord { get; set; }
    public string? SalaryText { get; set; }
    public string? Priority { get; set; }
    public string? FitRationale { get; set; }
    public string? GapsNotes { get; set; }
    public DateOnly? StatusDate { get; set; }
    public DateOnly? VerifiedDate { get; set; }
    public Guid Id { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public JobWorkMode WorkMode { get; set; } = JobWorkMode.Unknown;
    public JobLifecycleState LifecycleState { get; set; } = JobLifecycleState.Discovered;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public JobEvaluation? Evaluation { get; set; }
    public Application? Application { get; set; }
}
