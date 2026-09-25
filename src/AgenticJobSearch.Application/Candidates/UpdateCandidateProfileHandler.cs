using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Candidates;

public sealed class UpdateCandidateProfileHandler(ICandidateProfileRepository candidateProfiles)
{
    private static readonly string[] WorkModes = ["", "remote", "hybrid", "onsite", "flexible"];
    private static readonly string[] EmploymentTypes = ["", "full_time", "part_time", "contract", "internship", "flexible"];

    public async Task<CandidateProfileDto> HandleAsync(UpdateCandidateProfileRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var profile = await candidateProfiles.GetDefaultAsync(cancellationToken);
        profile.Headline = Clean(request.Headline);
        profile.ProfessionalSummary = Clean(request.ProfessionalSummary);
        profile.TargetLevel = Clean(request.TargetLevel);
        profile.TargetRoleFamilies = Clean(request.TargetRoleFamilies);
        profile.TargetIndustries = Clean(request.TargetIndustries);
        profile.PreferredLocations = Clean(request.PreferredLocations);
        profile.WorkModePreference = Clean(request.WorkModePreference);
        profile.EmploymentTypePreference = Clean(request.EmploymentTypePreference);
        profile.Skills = Clean(request.Skills);
        profile.WorkAuthorization = Clean(request.WorkAuthorization);
        profile.RequiresSponsorship = request.RequiresSponsorship;
        profile.LinkedInUrl = Clean(request.LinkedInUrl);
        profile.PortfolioUrl = Clean(request.PortfolioUrl);
        profile.ResumeText = Clean(request.ResumeText);
        profile.Institution = Clean(request.Institution);
        profile.Degree = Clean(request.Degree);
        profile.FieldOfStudy = Clean(request.FieldOfStudy);
        profile.GraduationYear = request.GraduationYear;
        profile.YearsExperience = request.YearsExperience;
        profile.RecentEmployer = Clean(request.RecentEmployer);
        profile.RecentJobTitle = Clean(request.RecentJobTitle);

        ApplyEvidence(profile, request.Evidence ?? []);

        await candidateProfiles.SaveAsync(profile, cancellationToken);
        return profile.ToDto();
    }

    private static void ApplyEvidence(CandidateProfile profile, IReadOnlyList<UpdateCandidateEvidenceRequest> requested)
    {
        while (profile.Evidence.Count > requested.Count) profile.Evidence.RemoveAt(profile.Evidence.Count - 1);
        for (var index = 0; index < requested.Count; index++)
        {
            var evidence = index < profile.Evidence.Count
                ? profile.Evidence[index]
                : AddEvidence(profile);
            var item = requested[index];
            evidence.Category = Clean(item.Category);
            evidence.Statement = Clean(item.Statement);
            evidence.VerificationStatus = item.VerificationStatus;
            evidence.Source = Clean(item.Source);
        }
    }

    private static CandidateEvidence AddEvidence(CandidateProfile profile)
    {
        var evidence = new CandidateEvidence { CandidateProfileId = profile.Id };
        profile.Evidence.Add(evidence);
        return evidence;
    }

    private static void Validate(UpdateCandidateProfileRequest request)
    {
        ValidateLength(request.Headline, 300, "Headline");
        ValidateLength(request.ProfessionalSummary, 3000, "Professional summary");
        ValidateLength(request.TargetLevel, 120, "Target level");
        ValidateLength(request.TargetRoleFamilies, 500, "Target roles");
        ValidateLength(request.TargetIndustries, 500, "Target industries");
        ValidateLength(request.PreferredLocations, 500, "Preferred locations");
        ValidateLength(request.Skills, 3000, "Skills");
        ValidateLength(request.WorkAuthorization, 200, "Work authorization");
        ValidateLength(request.ResumeText, 30000, "Resume text");
        ValidateLength(request.Institution, 300, "Institution");
        ValidateLength(request.Degree, 200, "Degree");
        ValidateLength(request.FieldOfStudy, 200, "Field of study");
        ValidateLength(request.RecentEmployer, 300, "Recent employer");
        ValidateLength(request.RecentJobTitle, 300, "Recent job title");
        ValidateChoice(request.WorkModePreference, WorkModes, "work mode");
        ValidateChoice(request.EmploymentTypePreference, EmploymentTypes, "employment type");
        ValidateUrl(request.LinkedInUrl, "LinkedIn URL");
        ValidateUrl(request.PortfolioUrl, "Portfolio URL");

        var latestGraduationYear = DateTime.UtcNow.Year + 10;
        if (request.GraduationYear is < 1900 || request.GraduationYear > latestGraduationYear)
            throw new CandidateProfileFailure(400, $"Graduation year must be between 1900 and {latestGraduationYear}.");
        if (request.YearsExperience is < 0 or > 70)
            throw new CandidateProfileFailure(400, "Years of experience must be between 0 and 70.");
        if (request.Evidence?.Count > 25)
            throw new CandidateProfileFailure(400, "A profile can contain at most 25 evidence statements.");

        foreach (var item in request.Evidence ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Category) || string.IsNullOrWhiteSpace(item.Statement))
                throw new CandidateProfileFailure(400, "Each evidence statement needs a category and statement.");
            ValidateLength(item.Category, 100, "Evidence category");
            ValidateLength(item.Statement, 1200, "Evidence statement");
            ValidateLength(item.Source, 300, "Evidence source");
        }
    }

    private static void ValidateChoice(string? value, string[] allowed, string field)
    {
        if (!allowed.Contains(Clean(value), StringComparer.Ordinal))
            throw new CandidateProfileFailure(400, $"Choose a valid {field}.");
    }

    private static void ValidateUrl(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new CandidateProfileFailure(400, $"{field} must be a complete http or https URL.");
        ValidateLength(value, 2048, field);
    }

    private static void ValidateLength(string? value, int maximum, string field)
    {
        if (value?.Length > maximum) throw new CandidateProfileFailure(400, $"{field} is too long.");
    }

    private static string Clean(string? value) => value?.Trim() ?? "";
}
