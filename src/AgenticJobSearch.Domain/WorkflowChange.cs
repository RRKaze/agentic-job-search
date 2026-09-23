namespace AgenticJobSearch.Domain;

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
