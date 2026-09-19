namespace AgenticJobSearch.Domain;

public sealed class Feedback
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public FeedbackType Type { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
