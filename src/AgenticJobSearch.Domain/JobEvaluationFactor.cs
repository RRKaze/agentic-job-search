namespace AgenticJobSearch.Domain;

public sealed class JobEvaluationFactor
{
    public Guid Id { get; set; }
    public Guid JobEvaluationId { get; set; }
    public JobEvaluation? JobEvaluation { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int ScoreImpact { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public List<JobEvaluationEvidenceSnapshot> Evidence { get; set; } = [];
}
