using AgenticJobSearch.Domain;
using System.Text.Json;

namespace AgenticJobSearch.Application.Jobs;

public sealed class JobScoringService
{
    public const string CurrentVersion = "deterministic-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JobEvaluation Evaluate(Job job, CandidateProfile candidateProfile)
    {
        var source = job.SourceText;
        var factors = new List<JobEvaluationFactor>();
        var fitScore = 45;
        var priority = 45;
        var eligibility = EligibilityDecision.Eligible;

        ApplyFactor(factors, "Backend/platform alignment", 20, KeywordImpact(source, 20, "backend", "platform", "api", "distributed", "service", "microservice"),
            "Rewards roles centered on backend, platform, API, or distributed systems work.");
        ApplyFactor(factors, "C#/.NET match", 18, KeywordImpact(source, 18, "c#", ".net", "asp.net", "dotnet"),
            "Matches verified production C#/.NET and ASP.NET Core experience.");
        ApplyFactor(factors, "Authentication or identity domain", 16, KeywordImpact(source, 16, "authentication", "identity", "oauth", "oidc", "sso", "authorization"),
            "Strongly matches verified authentication modernization experience.");
        ApplyFactor(factors, "SQL and data workflows", 10, KeywordImpact(source, 10, "sql", "postgres", "postgresql", "database", "data migration"),
            "Matches verified SQL and data remediation experience.");
        ApplyFactor(factors, "Frontend expectation", 6, KeywordImpact(source, 6, "react", "typescript", "javascript", "angular"),
            "Adds value when frontend work complements backend ownership.");

        if (JobNormalizer.ContainsAny(source, "sponsorship required", "visa sponsorship required", "must require sponsorship"))
        {
            eligibility = EligibilityDecision.NeedsReview;
            ApplyFactor(factors, "Sponsorship wording", 8, -8, "Candidate does not require sponsorship; unclear sponsorship wording should be reviewed.");
        }

        if (JobNormalizer.ContainsAny(source, "staff", "principal"))
        {
            ApplyFactor(factors, "Level mismatch risk", 12, -10, "Staff/Principal is not the primary target for this search.");
            priority -= 12;
        }

        if (JobNormalizer.ContainsAny(source, "junior", "entry level", "new grad"))
        {
            eligibility = EligibilityDecision.Ineligible;
            ApplyFactor(factors, "Seniority mismatch", 20, -25, "Junior or entry-level roles do not match the target level.");
        }

        if (JobNormalizer.ContainsAny(source, "24/7", "heavy on-call", "frequent on-call", "customer support rotation"))
        {
            ApplyFactor(factors, "Support/on-call load", 12, -14, "Heavy support or on-call expectations reduce application priority.");
            priority -= 18;
        }

        if (job.WorkMode == JobWorkMode.Remote || job.WorkMode == JobWorkMode.Hybrid)
        {
            ApplyFactor(factors, "Work mode", 8, 6, "Remote or hybrid work mode is acceptable for early targeting.");
            priority += 5;
        }

        fitScore += factors.Sum(factor => factor.ScoreImpact);
        priority += factors.Where(factor => factor.ScoreImpact > 0).Sum(factor => factor.ScoreImpact / 2);

        if (eligibility == EligibilityDecision.Ineligible)
        {
            priority = Math.Min(priority, 20);
        }

        fitScore = Clamp(fitScore);
        priority = Clamp(priority);
        AttachVerifiedEvidence(factors, candidateProfile);

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

    private static int KeywordImpact(string source, int impact, params string[] keywords)
    {
        return JobNormalizer.ContainsAny(source, keywords) ? impact : 0;
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

    private static void AttachVerifiedEvidence(IEnumerable<JobEvaluationFactor> factors, CandidateProfile profile)
    {
        var verified = profile.Evidence
            .Where(evidence => evidence.VerificationStatus == EvidenceVerificationStatus.Verified)
            .ToList();

        foreach (var factor in factors.Where(factor => factor.ScoreImpact > 0))
        {
            var keywords = EvidenceKeywords(factor.Name);
            if (keywords.Length == 0) continue;

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

    private static string[] EvidenceKeywords(string factorName) => factorName switch
    {
        "Backend/platform alignment" => ["backend", "platform", "api", "distributed", "service"],
        "C#/.NET match" => ["c#", ".net", "asp.net", "dotnet"],
        "Authentication or identity domain" => ["authentication", "identity", "oauth", "oidc", "sso", "authorization"],
        "SQL and data workflows" => ["sql", "postgres", "postgresql", "database", "data migration"],
        "Frontend expectation" => ["react", "typescript", "javascript", "angular"],
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
