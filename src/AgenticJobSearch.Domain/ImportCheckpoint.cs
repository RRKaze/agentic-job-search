namespace AgenticJobSearch.Domain;
public sealed class ImportCheckpoint
{
    public string Source { get; set; } = "";
    public string Commit { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public DateTimeOffset ImportedAt { get; set; }
}
