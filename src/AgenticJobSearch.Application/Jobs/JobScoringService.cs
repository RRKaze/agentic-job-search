using AgenticJobSearch.Domain;
using System.Text.Json;

namespace AgenticJobSearch.Application.Jobs;

public sealed class JobScoringService
{
    public const string CurrentVersion = "deterministic-v2";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JobEvaluation Evaluate(Job job, CandidateProfile candidateProfile)
    {
        var source = job.SourceText;
        var factors = new List<JobEvaluationFactor>();
        var fitScore = 45;
        var priority = 45;
        var eligibility = EligibilityDecision.Eligible;
        var comparisonText = $"{job.Title} {source}";
        var matchedRoles = MatchProfileTerms(comparisonText, candidateProfile.TargetRoleFamilies, splitWords: true);
        var matchedSkills = MatchProfileTerms(comparisonText, candidateProfile.Skills, splitWords: false);

        ApplyFactor(factors, "Target role alignment", 24, Math.Min(24, matchedRoles.Count * 12),
            matchedRoles.Count > 0
                ? $"Matches target role terms: {string.Join(", ", matchedRoles)}."
                : "No target role terms were found in the opportunity.");
        ApplyFactor(factors, "Skills alignment", 30, Math.Min(30, matchedSkills.Count * 10),
            matchedSkills.Count > 0
                ? $"Matches profile skills: {string.Join(", ", matchedSkills)}."
                : "No profile skills were found in the opportunity.");

        var excludesSponsorship = JobNormalizer.ContainsAny(source,
            "without current or future sponsorship", "without sponsorship", "no sponsorship",
            "unable to sponsor", "cannot sponsor", "will not sponsor", "must not require sponsorship");
        if (candidateProfile.RequiresSponsorship && excludesSponsorship)
        {
            eligibility = EligibilityDecision.Ineligible;
            ApplyFactor(factors, "Sponsorship eligibility", 30, -30,
                "The candidate requires sponsorship, but the opportunity explicitly excludes it.");
        }
        else if (JobNormalizer.ContainsAny(source, "sponsorship required", "visa sponsorship required", "must require sponsorship"))
        {
            eligibility = EligibilityDecision.NeedsReview;
            ApplyFactor(factors, "Sponsorship wording", 8, -8, "The opportunity's sponsorship wording should be reviewed.");
        }

        var requiresCitizenship = JobNormalizer.ContainsAny(source,
            "u.s. citizenship required", "us citizenship required", "must be a u.s. citizen", "must be a us citizen");
        if (requiresCitizenship && !JobNormalizer.ContainsAny(candidateProfile.WorkAuthorization, "citizen", "citizenship"))
        {
            if (eligibility != EligibilityDecision.Ineligible) eligibility = EligibilityDecision.NeedsReview;
            ApplyFactor(factors, "Work authorization", 20, -8,
                "The opportunity requires citizenship, but the saved work-authorization profile does not confirm it.");
        }

        var targetsEntryLevel = JobNormalizer.ContainsAny(candidateProfile.TargetLevel, "junior", "entry level", "new grad");
        var targetsSeniorLevel = JobNormalizer.ContainsAny(candidateProfile.TargetLevel, "senior");
        var targetsStaffLevel = JobNormalizer.ContainsAny(candidateProfile.TargetLevel, "staff", "principal");
        var isEntryLevelRole = JobNormalizer.ContainsAny(comparisonText, "junior", "entry level", "new grad");
        var isSeniorRole = ContainsWholeTerm(comparisonText, "senior");
        var isStaffRole = JobNormalizer.ContainsAny(comparisonText, "staff", "principal");

        if (targetsEntryLevel && (isSeniorRole || isStaffRole))
        {
            eligibility = EligibilityDecision.Ineligible;
            ApplyFactor(factors, "Seniority mismatch", 20, -25,
                "Senior, Staff, or Principal roles do not match the candidate's entry-level target.");
        }
        else if (isStaffRole && !targetsStaffLevel)
        {
            ApplyFactor(factors, "Level mismatch risk", 12, -10, "Staff/Principal is not the primary target for this search.");
            priority -= 12;
        }
        else if (isSeniorRole && targetsSeniorLevel)
        {
            ApplyFactor(factors, "Target level alignment", 20, 8, "The role matches the candidate's target level.");
        }

        if (isEntryLevelRole)
        {
            if (targetsEntryLevel)
            {
                ApplyFactor(factors, "Target level alignment", 20, 10, "The role matches the candidate's target level.");
            }
            else
            {
                eligibility = EligibilityDecision.Ineligible;
                ApplyFactor(factors, "Seniority mismatch", 20, -25, "Junior or entry-level roles do not match the candidate's target level.");
            }
        }

        if (JobNormalizer.ContainsAny(source, "24/7", "heavy on-call", "frequent on-call", "customer support rotation"))
        {
            ApplyFactor(factors, "Support/on-call load", 12, -14, "Heavy support or on-call expectations reduce application priority.");
            priority -= 18;
        }

        if (!string.IsNullOrWhiteSpace(candidateProfile.WorkModePreference) &&
            !JobNormalizer.ContainsAny(candidateProfile.WorkModePreference, "flexible", "any", "no preference"))
        {
            if (WorkModeMatches(candidateProfile.WorkModePreference, job.WorkMode))
            {
                ApplyFactor(factors, "Work mode preference", 10, 6, "The opportunity matches the candidate's work-mode preference.");
                priority += 8;
            }
            else if (job.WorkMode != JobWorkMode.Unknown)
            {
                ApplyFactor(factors, "Work mode preference", 10, -4, "The opportunity does not match the candidate's work-mode preference.");
                priority -= 12;
            }
        }

        if (job.WorkMode != JobWorkMode.Remote &&
            !string.IsNullOrWhiteSpace(job.Location) &&
            !string.IsNullOrWhiteSpace(candidateProfile.PreferredLocations))
        {
            var matchedLocations = MatchProfileTerms(job.Location, candidateProfile.PreferredLocations, splitWords: false);
            if (matchedLocations.Count > 0)
            {
                ApplyFactor(factors, "Location preference", 8, 4,
                    $"Matches preferred location terms: {string.Join(", ", matchedLocations)}.");
                priority += 6;
            }
            else if (!JobNormalizer.ContainsAny(candidateProfile.PreferredLocations, "anywhere", "nationwide", "no preference"))
            {
                ApplyFactor(factors, "Location preference", 8, -3,
                    "The opportunity location does not match the candidate's preferred locations.");
                priority -= 6;
            }
        }

        var isContractRole = JobNormalizer.ContainsAny(source,
            "contract role", "contract position", "contractor", "temporary role", "six month contract", "6 month contract");
        var isPartTimeRole = JobNormalizer.ContainsAny(source, "part-time", "part time");
        if (!string.IsNullOrWhiteSpace(candidateProfile.EmploymentTypePreference) && (isContractRole || isPartTimeRole))
        {
            var preferenceMatches = isContractRole
                ? JobNormalizer.ContainsAny(candidateProfile.EmploymentTypePreference, "contract", "temporary")
                : JobNormalizer.ContainsAny(candidateProfile.EmploymentTypePreference, "part_time", "part-time", "part time");
            if (preferenceMatches)
            {
                ApplyFactor(factors, "Employment type preference", 8, 4,
                    "The opportunity matches the candidate's employment-type preference.");
                priority += 6;
            }
            else if (JobNormalizer.ContainsAny(candidateProfile.EmploymentTypePreference, "full_time", "full-time", "full time"))
            {
                ApplyFactor(factors, "Employment type preference", 8, -4,
                    "The opportunity does not match the candidate's full-time employment preference.");
                priority -= 8;
            }
        }

        fitScore += factors.Sum(factor => factor.ScoreImpact);
        priority += factors.Where(factor => factor.ScoreImpact > 0).Sum(factor => factor.ScoreImpact / 2);

        if (eligibility == EligibilityDecision.Ineligible)
        {
            priority = Math.Min(priority, 20);
        }

        fitScore = Clamp(fitScore);
        priority = Clamp(priority);
        AttachVerifiedEvidence(factors, candidateProfile, matchedRoles, matchedSkills);

        return new JobEvaluation
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Eligibility = eligibility,
            FitScore = fitScore,
            ApplicationPriority = priority,
            Recommendation = BuildRecommendation(eligibility, fitScore, priority),
            Explanation = BuildExplanation(job, candidateProfile, eligibility, fitScore, priority, factors),
            ScoringVersion = CurrentVersion,
            ProfileSnapshot = JsonSerializer.Serialize(CandidateScoringSnapshot.From(candidateProfile), JsonOptions),
            Factors = factors,
            EvaluatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void ApplyFactor(List<JobEvaluationFactor> factors, string name, int weight, int impact, string rationale)
    {
        factors.Add(new JobEvaluationFactor
        {
            Id = Guid.NewGuid(),
            Name = name,
            Weight = weight,
            ScoreImpact = impact,
            Rationale = rationale
        });
    }

    private static IReadOnlyList<string> MatchProfileTerms(string source, string configuredTerms, bool splitWords)
    {
        var terms = configuredTerms
            .Split([',', ';', '\n', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(term => splitWords ? term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : [term])
            .Where(term => term.Length >= 2)
            .Where(term => !splitWords || !GenericRoleWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return terms.Where(term => ContainsWholeTerm(source, term)).ToList();
    }

    private static bool WorkModeMatches(string preference, JobWorkMode workMode) => workMode switch
    {
        JobWorkMode.Remote => JobNormalizer.ContainsAny(preference, "remote"),
        JobWorkMode.Hybrid => JobNormalizer.ContainsAny(preference, "hybrid"),
        JobWorkMode.Onsite => JobNormalizer.ContainsAny(preference, "onsite", "on-site", "office"),
        _ => false
    };

    private static readonly HashSet<string> GenericRoleWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "engineer", "engineering", "manager", "management", "specialist", "associate", "software"
    };

    private static void AttachVerifiedEvidence(
        IEnumerable<JobEvaluationFactor> factors,
        CandidateProfile profile,
        IReadOnlyList<string> matchedRoles,
        IReadOnlyList<string> matchedSkills)
    {
        var verified = profile.Evidence
            .Where(evidence => evidence.VerificationStatus == EvidenceVerificationStatus.Verified)
            .ToList();

        foreach (var factor in factors.Where(factor => factor.ScoreImpact > 0))
        {
            var keywords = EvidenceKeywords(factor.Name, matchedRoles, matchedSkills);
            if (keywords.Count == 0) continue;

            factor.Evidence = verified
                .Where(evidence => keywords.Any(keyword => ContainsWholeTerm(
                    $"{evidence.Category} {evidence.Statement}", keyword)))
                .Select(evidence => new JobEvaluationEvidenceSnapshot
                {
                    Id = Guid.NewGuid(),
                    SourceEvidenceId = evidence.Id,
                    Category = evidence.Category,
                    Statement = evidence.Statement,
                    Source = evidence.Source
                })
                .ToList();
        }
    }

    private static bool ContainsWholeTerm(string source, string term)
    {
        for (var start = 0; start < source.Length;)
        {
            var index = source.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;

            var end = index + term.Length;
            var startsAtBoundary = index == 0 || !char.IsLetterOrDigit(source[index - 1]);
            var endsAtBoundary = end == source.Length || !char.IsLetterOrDigit(source[end]);
            if (startsAtBoundary && endsAtBoundary) return true;
            start = index + 1;
        }

        return false;
    }

    private static IReadOnlyList<string> EvidenceKeywords(
        string factorName,
        IReadOnlyList<string> matchedRoles,
        IReadOnlyList<string> matchedSkills) => factorName switch
    {
        "Target role alignment" => matchedRoles,
        "Skills alignment" => matchedSkills,
        _ => []
    };

    private static string BuildRecommendation(EligibilityDecision eligibility, int fitScore, int priority)
    {
        if (eligibility == EligibilityDecision.Ineligible)
        {
            return "Do not apply";
        }

        if (fitScore >= 78 && priority >= 70)
        {
            return "Strong target";
        }

        if (fitScore >= 65)
        {
            return "Review and consider";
        }

        return "Low priority";
    }

    private static string BuildExplanation(
        Job job,
        CandidateProfile candidateProfile,
        EligibilityDecision eligibility,
        int fitScore,
        int priority,
        IReadOnlyCollection<JobEvaluationFactor> factors)
    {
        var positive = factors
            .Where(factor => factor.ScoreImpact > 0)
            .OrderByDescending(factor => factor.ScoreImpact)
            .Take(3)
            .Select(factor => factor.Name.ToLowerInvariant());

        var risks = factors
            .Where(factor => factor.ScoreImpact < 0)
            .OrderBy(factor => factor.ScoreImpact)
            .Take(2)
            .Select(factor => factor.Name.ToLowerInvariant());

        var positiveText = positive.Any() ? string.Join(", ", positive) : "limited direct keyword evidence";
        var riskText = risks.Any() ? $" Main risks: {string.Join(", ", risks)}." : string.Empty;

        return $"{job.Title} at {job.Company} is {eligibility.ToString().ToLowerInvariant()} with fit {fitScore}/100 and priority {priority}/100 for {candidateProfile.TargetLevel} targeting {candidateProfile.TargetRoleFamilies}. Strongest signals: {positiveText}.{riskText}";
    }

    private static int Clamp(int value)
    {
        return Math.Clamp(value, 0, 100);
    }
}
