using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Candidates;

public sealed record CandidateProfileDto(
    Guid Id,
    string DisplayName,
    string Headline,
    string ProfessionalSummary,
    string TargetLevel,
    string TargetRoleFamilies,
    string TargetIndustries,
    string PreferredLocations,
    string WorkModePreference,
    string EmploymentTypePreference,
    string Skills,
    string WorkAuthorization,
    bool RequiresSponsorship,
    string LinkedInUrl,
    string PortfolioUrl,
    string ResumeText,
    string Institution,
    string Degree,
    string FieldOfStudy,
    int? GraduationYear,
    int? YearsExperience,
    string RecentEmployer,
    string RecentJobTitle,
    IReadOnlyList<CandidateEvidenceDto> Evidence);

public sealed record CandidateEvidenceDto(
    string Category,
    string Statement,
    EvidenceVerificationStatus VerificationStatus,
    string Source);

public sealed record UpdateCandidateProfileRequest(
    string? Headline,
    string? ProfessionalSummary,
    string? TargetLevel,
    string? TargetRoleFamilies,
    string? TargetIndustries,
    string? PreferredLocations,
    string? WorkModePreference,
    string? EmploymentTypePreference,
    string? Skills,
    string? WorkAuthorization,
    bool RequiresSponsorship,
    string? LinkedInUrl,
    string? PortfolioUrl,
    string? ResumeText,
    string? Institution,
    string? Degree,
    string? FieldOfStudy,
    int? GraduationYear,
    int? YearsExperience,
    string? RecentEmployer,
    string? RecentJobTitle,
    IReadOnlyList<UpdateCandidateEvidenceRequest>? Evidence);

public sealed record UpdateCandidateEvidenceRequest(
    string? Category,
    string? Statement,
    EvidenceVerificationStatus VerificationStatus,
    string? Source);

public sealed class CandidateProfileFailure(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
