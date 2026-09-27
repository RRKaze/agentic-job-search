namespace AgenticJobSearch.Domain;

public sealed class JobEvaluation
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job? Job { get; set; }
    public EligibilityDecision Eligibility { get; set; } = EligibilityDecision.NeedsReview;
    public int FitScore { get; set; }
    public int ApplicationPriority { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string ScoringVersion { get; set; } = string.Empty;
    public string ProfileSnapshot { get; set; } = "{}";
    public List<JobEvaluationFactor> Factors { get; set; } = [];
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
}
