namespace AgenticJobSearch.Domain;

public sealed class JobEvaluationEvidenceSnapshot
{
    public Guid Id { get; set; }
    public Guid JobEvaluationFactorId { get; set; }
    public JobEvaluationFactor? JobEvaluationFactor { get; set; }
    public Guid SourceEvidenceId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
