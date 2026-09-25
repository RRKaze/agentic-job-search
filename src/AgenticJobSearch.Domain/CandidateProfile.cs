namespace AgenticJobSearch.Domain;

public sealed class CandidateProfile
{
    public Guid? OwnerId { get; set; }
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string ProfessionalSummary { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;
    public string TargetRoleFamilies { get; set; } = string.Empty;
    public string TargetIndustries { get; set; } = string.Empty;
    public string PreferredLocations { get; set; } = string.Empty;
    public string WorkModePreference { get; set; } = string.Empty;
    public string EmploymentTypePreference { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string WorkAuthorization { get; set; } = string.Empty;
    public bool RequiresSponsorship { get; set; }
    public string LinkedInUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
    public string ResumeText { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public int? GraduationYear { get; set; }
    public int? YearsExperience { get; set; }
    public string RecentEmployer { get; set; } = string.Empty;
    public string RecentJobTitle { get; set; } = string.Empty;
    public List<CandidateEvidence> Evidence { get; set; } = [];
}
