namespace AgenticJobSearch.Domain;

public sealed class Application
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job? Job { get; set; }
    public ApplicationState State { get; set; } = ApplicationState.NotStarted;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public List<Feedback> Feedback { get; set; } = [];
}
