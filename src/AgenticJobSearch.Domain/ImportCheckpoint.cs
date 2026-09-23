namespace AgenticJobSearch.Domain;
public sealed class ImportCheckpoint
{
    public string Source { get; set; } = "";
    public string Commit { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public DateTimeOffset ImportedAt { get; set; }
}
public sealed class TrackingChange
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";
    public string Entity { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? Previous { get; set; }
    public string Current { get; set; } = "";
    public string Commit { get; set; } = "";
    public DateTimeOffset ObservedAt { get; set; }
}

public sealed class WorkflowChange
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid JobId { get; set; }
    public Job? Job { get; set; }
    public string PreviousStatus { get; set; } = "";
    public string CurrentStatus { get; set; } = "";
    public DateTimeOffset ChangedAt { get; set; }
}
