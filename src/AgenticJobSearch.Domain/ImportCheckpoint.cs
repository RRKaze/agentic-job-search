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
