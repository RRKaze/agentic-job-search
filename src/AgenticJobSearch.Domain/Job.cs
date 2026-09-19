namespace AgenticJobSearch.Domain;

public sealed class Job
{
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
