using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Candidates;

internal static class CandidateProfileMappings
{
    public static CandidateProfileDto ToDto(this CandidateProfile profile) => new(
        profile.Id,
        profile.DisplayName,
        profile.Headline,
        profile.ProfessionalSummary,
        profile.TargetLevel,
        profile.TargetRoleFamilies,
        profile.TargetIndustries,
        profile.PreferredLocations,
        profile.WorkModePreference,
        profile.EmploymentTypePreference,
        profile.Skills,
        profile.WorkAuthorization,
        profile.RequiresSponsorship,
        profile.LinkedInUrl,
        profile.PortfolioUrl,
        profile.ResumeText,
        profile.Institution,
        profile.Degree,
        profile.FieldOfStudy,
        profile.GraduationYear,
        profile.YearsExperience,
        profile.RecentEmployer,
        profile.RecentJobTitle,
        profile.Evidence.Select(item => new CandidateEvidenceDto(
            item.Category,
            item.Statement,
            item.VerificationStatus,
            item.Source)).ToList());
}
