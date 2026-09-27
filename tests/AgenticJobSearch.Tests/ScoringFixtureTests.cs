using System.Text.Json;
using System.Text.Json.Serialization;
using AgenticJobSearch.Application.Jobs;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Tests;

public sealed class ScoringFixtureTests
{
    [Fact]
    public void Versioned_fixture_set_preserves_expected_ranking_behavior()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "scoring-v2.json");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var fixtureSet = JsonSerializer.Deserialize<ScoringFixtureSet>(File.ReadAllText(path), options);

        Assert.NotNull(fixtureSet);
        Assert.Equal(JobScoringService.CurrentVersion, fixtureSet.Version);
        Assert.NotEmpty(fixtureSet.Profiles);
        Assert.NotEmpty(fixtureSet.Fixtures);
        Assert.Equal(fixtureSet.Fixtures.Count, fixtureSet.Fixtures.Select(fixture => fixture.Name).Distinct().Count());

        foreach (var fixture in fixtureSet.Fixtures)
        {
            var profile = Assert.Contains(fixture.Profile, fixtureSet.Profiles);
            var result = new JobScoringService().Evaluate(fixture.Job.ToDomain(), profile.ToDomain());

            Assert.True(result.FitScore >= fixture.Expected.MinFitScore && result.FitScore <= fixture.Expected.MaxFitScore,
                $"{fixture.Name}: fit score {result.FitScore} was outside {fixture.Expected.MinFitScore}-{fixture.Expected.MaxFitScore}.");
            Assert.True(result.ApplicationPriority >= fixture.Expected.MinPriority && result.ApplicationPriority <= fixture.Expected.MaxPriority,
                $"{fixture.Name}: priority {result.ApplicationPriority} was outside {fixture.Expected.MinPriority}-{fixture.Expected.MaxPriority}.");
            Assert.Equal(fixture.Expected.Eligibility, result.Eligibility);
            Assert.Equal(fixture.Expected.Recommendation, result.Recommendation);
            foreach (var factor in fixture.Expected.RequiredFactors)
                Assert.Contains(result.Factors, actual => actual.Name == factor);
        }
    }

    private sealed record ScoringFixtureSet(
        string Version,
        IReadOnlyDictionary<string, CandidateFixture> Profiles,
        IReadOnlyList<ScoringFixture> Fixtures);
    private sealed record ScoringFixture(string Name, string Profile, JobFixture Job, ExpectedFixture Expected);
    private sealed record CandidateFixture(
        string TargetLevel,
        string TargetRoleFamilies,
        string PreferredLocations,
        string WorkModePreference,
        string EmploymentTypePreference,
        string Skills,
        string WorkAuthorization,
        bool RequiresSponsorship,
        IReadOnlyList<EvidenceFixture> Evidence)
    {
        public CandidateProfile ToDomain() => new()
        {
            Id = Guid.NewGuid(),
            TargetLevel = TargetLevel,
            TargetRoleFamilies = TargetRoleFamilies,
            PreferredLocations = PreferredLocations,
            WorkModePreference = WorkModePreference,
            EmploymentTypePreference = EmploymentTypePreference,
            Skills = Skills,
            WorkAuthorization = WorkAuthorization,
            RequiresSponsorship = RequiresSponsorship,
            Evidence = Evidence.Select(item => item.ToDomain()).ToList()
        };
    }

    private sealed record EvidenceFixture(
        string Category,
        string Statement,
        EvidenceVerificationStatus VerificationStatus,
        string Source)
    {
        public CandidateEvidence ToDomain() => new()
        {
            Id = Guid.NewGuid(),
            Category = Category,
            Statement = Statement,
            VerificationStatus = VerificationStatus,
            Source = Source
        };
    }

    private sealed record JobFixture(string Title, string Company, string? Location, string SourceText, JobWorkMode WorkMode)
    {
        public Job ToDomain() => new()
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Company = Company,
            Location = Location ?? string.Empty,
            SourceText = SourceText,
            WorkMode = WorkMode
        };
    }

    private sealed record ExpectedFixture(
        EligibilityDecision Eligibility,
        int MinFitScore,
        int MaxFitScore,
        int MinPriority,
        int MaxPriority,
        string Recommendation,
        IReadOnlyList<string> RequiredFactors);
}
