using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed record JobDto(
    Guid Id,
    string? ExternalId,
    string Title,
    string Company,
    string Location,
    string? SourceUrl,
    string? TrackingStage,
    string? SalaryText,
    string? Priority,
    string? FitRationale,
    string? GapsNotes,
    DateOnly? StatusDate,
    DateOnly? VerifiedDate,
    ApplicationSummaryDto? Application,
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

public sealed record ApplicationSummaryDto(
    string? ExternalId,
    ApplicationState State,
    DateOnly? SubmittedDate,
    string? ResumeVersion,
    DateOnly? NextFollowUp,
    string? Outcome,
    string? Notes);

public sealed record JobEvaluationFactorDto(
    string Name,
    int Weight,
    int ScoreImpact,
    string Rationale);
