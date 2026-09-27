using AgenticJobSearch.Application.Jobs;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Tests;

public sealed class JobScoringServiceTests
{
    [Fact]
    public void Evaluate_rewards_backend_identity_and_dotnet_alignment()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Senior Backend Engineer",
            Company = "ExampleCo",
            SourceText = "Senior Backend Engineer building authentication APIs in C# .NET with SQL, distributed services, and remote work.",
            WorkMode = JobWorkMode.Remote
        };

        var result = new JobScoringService().Evaluate(job, Candidate());

        Assert.Equal(EligibilityDecision.Eligible, result.Eligibility);
        Assert.True(result.FitScore >= 90);
        Assert.True(result.ApplicationPriority >= 75);
        Assert.Equal("Strong target", result.Recommendation);
    }

    [Fact]
    public void Evaluate_penalizes_junior_roles_as_ineligible()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Junior Software Engineer",
            Company = "ExampleCo",
            SourceText = "Junior Software Engineer entry level role with React.",
            WorkMode = JobWorkMode.Remote
        };

        var result = new JobScoringService().Evaluate(job, Candidate());

        Assert.Equal(EligibilityDecision.Ineligible, result.Eligibility);
        Assert.Equal("Do not apply", result.Recommendation);
        Assert.True(result.ApplicationPriority <= 20);
    }

    [Fact]
    public void Evaluate_keeps_fit_and_priority_separate_for_heavy_on_call()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Senior Platform Engineer",
            Company = "ExampleCo",
            SourceText = "Senior Platform Engineer working on backend distributed services in C# .NET. Includes 24/7 heavy on-call and customer support rotation.",
            WorkMode = JobWorkMode.Remote
        };

        var result = new JobScoringService().Evaluate(job, Candidate());

        Assert.Equal(EligibilityDecision.Eligible, result.Eligibility);
        Assert.True(result.FitScore > result.ApplicationPriority);
        Assert.Contains(result.Factors, factor => factor.Name == "Support/on-call load" && factor.ScoreImpact < 0);
    }

    [Fact]
    public void Evaluate_does_not_attribute_evidence_from_partial_keyword_matches()
    {
        var candidate = Candidate();
        candidate.Evidence =
        [
            new CandidateEvidence
            {
                Id = Guid.NewGuid(), Category = "Planning", Statement = "Led annual capital planning.",
                VerificationStatus = EvidenceVerificationStatus.Verified, Source = "Review"
            },
            new CandidateEvidence
            {
                Id = Guid.NewGuid(), Category = "Backend", Statement = "Built C# APIs.",
                VerificationStatus = EvidenceVerificationStatus.Verified, Source = "Resume"
            }
        ];
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Backend Engineer", Company = "ExampleCo",
            SourceText = "Build backend APIs in C#.", WorkMode = JobWorkMode.Remote
        };

        var result = new JobScoringService().Evaluate(job, candidate);
        var backend = Assert.Single(result.Factors, factor => factor.Name == "Backend/platform alignment");

        Assert.Single(backend.Evidence);
        Assert.Equal("Built C# APIs.", backend.Evidence[0].Statement);
    }

    private static CandidateProfile Candidate()
    {
        return new CandidateProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test candidate",
            TargetLevel = "Senior Software Engineer",
            TargetRoleFamilies = "Backend, Platform, Identity/Authentication, Distributed Systems"
        };
    }
}
