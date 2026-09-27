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
        var backend = Assert.Single(result.Factors, factor => factor.Name == "Skills alignment");

        Assert.Single(backend.Evidence);
        Assert.Equal("Built C# APIs.", backend.Evidence[0].Statement);
    }

    [Fact]
    public void Evaluate_uses_profile_target_level_for_junior_roles()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Junior Software Engineer",
            Company = "ExampleCo",
            SourceText = "Junior entry level software engineer building React and TypeScript applications.",
            WorkMode = JobWorkMode.Remote
        };
        var entryLevel = new CandidateProfile
        {
            Id = Guid.NewGuid(), TargetLevel = "Entry Level", TargetRoleFamilies = "Software Engineering",
            Skills = "React, TypeScript", WorkModePreference = "remote"
        };
        var senior = Candidate();

        var entryResult = new JobScoringService().Evaluate(job, entryLevel);
        var seniorResult = new JobScoringService().Evaluate(job, senior);

        Assert.Equal(EligibilityDecision.Eligible, entryResult.Eligibility);
        Assert.NotEqual("Do not apply", entryResult.Recommendation);
        Assert.Equal(EligibilityDecision.Ineligible, seniorResult.Eligibility);
        Assert.True(entryResult.FitScore > seniorResult.FitScore);
    }

    [Fact]
    public void Evaluate_uses_profile_roles_and_skills_instead_of_a_fixed_software_stack()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Platform Engineer", Company = "ExampleCo",
            SourceText = "Build platform services using Go and Kubernetes.", WorkMode = JobWorkMode.Hybrid
        };
        var platform = new CandidateProfile
        {
            Id = Guid.NewGuid(), TargetLevel = "Senior", TargetRoleFamilies = "Platform Engineering",
            Skills = "Go, Kubernetes", WorkModePreference = "hybrid"
        };
        var operations = new CandidateProfile
        {
            Id = Guid.NewGuid(), TargetLevel = "Manager", TargetRoleFamilies = "Program Management",
            Skills = "Budgeting, Vendor Management", WorkModePreference = "hybrid"
        };

        var platformResult = new JobScoringService().Evaluate(job, platform);
        var operationsResult = new JobScoringService().Evaluate(job, operations);

        Assert.True(platformResult.FitScore >= operationsResult.FitScore + 20);
        Assert.Contains(platformResult.Factors, factor => factor.Name == "Target role alignment" && factor.ScoreImpact > 0);
        Assert.Contains(platformResult.Factors, factor => factor.Name == "Skills alignment" && factor.ScoreImpact > 0);
    }

    [Fact]
    public void Evaluate_uses_work_mode_preference_to_set_application_priority()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Senior Backend Engineer", Company = "ExampleCo",
            SourceText = "Build backend APIs in C# and .NET.", WorkMode = JobWorkMode.Onsite
        };
        var onsite = Candidate();
        onsite.WorkModePreference = "onsite";
        var remote = Candidate();
        remote.WorkModePreference = "remote";

        var onsiteResult = new JobScoringService().Evaluate(job, onsite);
        var remoteResult = new JobScoringService().Evaluate(job, remote);

        Assert.True(onsiteResult.ApplicationPriority >= remoteResult.ApplicationPriority + 15);
        Assert.Contains(remoteResult.Factors, factor => factor.Name == "Work mode preference" && factor.ScoreImpact < 0);
    }

    [Fact]
    public void Evaluate_uses_sponsorship_need_for_explicit_no_sponsorship_roles()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Backend Engineer", Company = "ExampleCo",
            SourceText = "Applicants must be authorized to work without current or future sponsorship.",
            WorkMode = JobWorkMode.Remote
        };
        var needsSponsorship = Candidate();
        needsSponsorship.RequiresSponsorship = true;
        var doesNotNeedSponsorship = Candidate();
        doesNotNeedSponsorship.RequiresSponsorship = false;

        var blocked = new JobScoringService().Evaluate(job, needsSponsorship);
        var eligible = new JobScoringService().Evaluate(job, doesNotNeedSponsorship);

        Assert.Equal(EligibilityDecision.Ineligible, blocked.Eligibility);
        Assert.Equal("Do not apply", blocked.Recommendation);
        Assert.Equal(EligibilityDecision.Eligible, eligible.Eligibility);
        Assert.Contains(blocked.Factors, factor => factor.Name == "Sponsorship eligibility" && factor.ScoreImpact < 0);
    }

    [Fact]
    public void Evaluate_uses_preferred_locations_for_non_remote_roles()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Senior Backend Engineer", Company = "ExampleCo",
            Location = "Boston, MA", SourceText = "Build backend APIs in C# and .NET.",
            WorkMode = JobWorkMode.Onsite
        };
        var boston = Candidate();
        boston.WorkModePreference = "onsite";
        boston.PreferredLocations = "Boston, MA";
        var newYork = Candidate();
        newYork.WorkModePreference = "onsite";
        newYork.PreferredLocations = "New York, NY";

        var matching = new JobScoringService().Evaluate(job, boston);
        var mismatch = new JobScoringService().Evaluate(job, newYork);

        Assert.True(matching.ApplicationPriority >= mismatch.ApplicationPriority + 10);
        Assert.Contains(mismatch.Factors, factor => factor.Name == "Location preference" && factor.ScoreImpact < 0);
    }

    [Fact]
    public void Evaluate_uses_employment_type_preference_for_contract_roles()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Backend Engineer", Company = "ExampleCo",
            SourceText = "Six month contract role building backend APIs.", WorkMode = JobWorkMode.Remote
        };
        var contract = Candidate();
        contract.EmploymentTypePreference = "contract";
        var fullTime = Candidate();
        fullTime.EmploymentTypePreference = "full_time";

        var matching = new JobScoringService().Evaluate(job, contract);
        var mismatch = new JobScoringService().Evaluate(job, fullTime);

        Assert.True(matching.ApplicationPriority >= mismatch.ApplicationPriority + 10);
        Assert.Contains(mismatch.Factors, factor => factor.Name == "Employment type preference" && factor.ScoreImpact < 0);
    }

    [Fact]
    public void Evaluate_flags_citizenship_restrictions_when_authorization_is_not_specific()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Backend Engineer", Company = "ExampleCo",
            SourceText = "Build backend APIs. U.S. citizenship required.", WorkMode = JobWorkMode.Remote
        };
        var generallyAuthorized = Candidate();
        generallyAuthorized.WorkAuthorization = "Authorized to work in the United States";
        var citizen = Candidate();
        citizen.WorkAuthorization = "U.S. citizen";

        var review = new JobScoringService().Evaluate(job, generallyAuthorized);
        var eligible = new JobScoringService().Evaluate(job, citizen);

        Assert.Equal(EligibilityDecision.NeedsReview, review.Eligibility);
        Assert.Equal(EligibilityDecision.Eligible, eligible.Eligibility);
        Assert.Contains(review.Factors, factor => factor.Name == "Work authorization" && factor.ScoreImpact < 0);
    }

    [Fact]
    public void Evaluate_rejects_senior_roles_for_an_entry_level_target()
    {
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Senior Software Engineer", Company = "ExampleCo",
            SourceText = "Senior engineer building React and TypeScript applications.", WorkMode = JobWorkMode.Remote
        };
        var entryLevel = new CandidateProfile
        {
            Id = Guid.NewGuid(), TargetLevel = "Entry Level", TargetRoleFamilies = "Software Engineering",
            Skills = "React, TypeScript", WorkModePreference = "remote"
        };

        var result = new JobScoringService().Evaluate(job, entryLevel);

        Assert.Equal(EligibilityDecision.Ineligible, result.Eligibility);
        Assert.Equal("Do not apply", result.Recommendation);
        Assert.Contains(result.Factors, factor => factor.Name == "Seniority mismatch" && factor.ScoreImpact < 0);
    }

    private static CandidateProfile Candidate()
    {
        return new CandidateProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test candidate",
            TargetLevel = "Senior Software Engineer",
            TargetRoleFamilies = "Backend, Platform, Identity/Authentication, Distributed Systems",
            Skills = "C#, .NET, SQL, Authentication, Distributed Systems",
            WorkModePreference = "remote"
        };
    }
}
