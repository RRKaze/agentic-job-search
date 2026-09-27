using System.Text.Json;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed class CandidateScoringSnapshot
{
    public Guid CandidateProfileId { get; init; }
    public string TargetLevel { get; init; } = string.Empty;
    public string TargetRoleFamilies { get; init; } = string.Empty;
    public string TargetIndustries { get; init; } = string.Empty;
    public string PreferredLocations { get; init; } = string.Empty;
    public string WorkModePreference { get; init; } = string.Empty;
    public string EmploymentTypePreference { get; init; } = string.Empty;
    public string Skills { get; init; } = string.Empty;
    public string WorkAuthorization { get; init; } = string.Empty;
    public bool RequiresSponsorship { get; init; }
    public int? YearsExperience { get; init; }

    public static CandidateScoringSnapshot From(CandidateProfile profile) => new()
    {
        CandidateProfileId = profile.Id,
        TargetLevel = profile.TargetLevel,
        TargetRoleFamilies = profile.TargetRoleFamilies,
        TargetIndustries = profile.TargetIndustries,
        PreferredLocations = profile.PreferredLocations,
        WorkModePreference = profile.WorkModePreference,
        EmploymentTypePreference = profile.EmploymentTypePreference,
        Skills = profile.Skills,
        WorkAuthorization = profile.WorkAuthorization,
        RequiresSponsorship = profile.RequiresSponsorship,
        YearsExperience = profile.YearsExperience
    };
}

public sealed record JobEvaluationDto(
    Guid Id,
    string ScoringVersion,
    EligibilityDecision Eligibility,
    int FitScore,
    int ApplicationPriority,
    string Recommendation,
    string Explanation,
    CandidateScoringSnapshot ProfileSnapshot,
    IReadOnlyList<JobEvaluationFactorDto> Factors,
    DateTimeOffset EvaluatedAt);

public static class JobEvaluationMappings
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static JobEvaluationDto ToDto(this JobEvaluation evaluation) => new(
        evaluation.Id,
        evaluation.ScoringVersion,
        evaluation.Eligibility,
        evaluation.FitScore,
        evaluation.ApplicationPriority,
        evaluation.Recommendation,
        evaluation.Explanation,
        JsonSerializer.Deserialize<CandidateScoringSnapshot>(evaluation.ProfileSnapshot, JsonOptions) ?? new CandidateScoringSnapshot(),
        evaluation.Factors
            .OrderByDescending(factor => Math.Abs(factor.ScoreImpact))
            .ThenBy(factor => factor.Name)
            .Select(factor => new JobEvaluationFactorDto(
                factor.Name,
                factor.Weight,
                factor.ScoreImpact,
                factor.Rationale,
                factor.Evidence.Select(item => new JobEvaluationEvidenceDto(
                    item.SourceEvidenceId,
                    item.Category,
                    item.Statement,
                    item.Source)).ToList()))
            .ToList(),
        evaluation.EvaluatedAt);
}

public sealed class EvaluationFailure(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
