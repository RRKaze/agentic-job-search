using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed record JobDto(
    Guid Id,
    string Title,
    string Company,
    string Location,
    string Seniority,
    JobWorkMode WorkMode,
    JobLifecycleState LifecycleState,
    EligibilityDecision Eligibility,
    int FitScore,
    int ApplicationPriority,
    string Recommendation,
    string Explanation,
    IReadOnlyList<JobEvaluationFactorDto> Factors,
    DateTimeOffset CreatedAt);

public sealed record JobEvaluationFactorDto(
    string Name,
    int Weight,
    int ScoreImpact,
    string Rationale);
