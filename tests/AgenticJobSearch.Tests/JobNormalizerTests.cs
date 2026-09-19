using AgenticJobSearch.Application.Jobs;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Tests;

public sealed class JobNormalizerTests
{
    [Fact]
    public void Normalize_uses_explicit_fields_before_inference()
    {
        var request = new AddJobRequest(
            "Title: Senior Backend Engineer\nCompany: ExampleCo\nLocation: Remote US\nBuild APIs in .NET.",
            Title: "Senior Platform Engineer",
            Company: "OverrideCo",
            Location: "Remote");

        var result = new JobNormalizer().Normalize(request);

        Assert.Equal("Senior Platform Engineer", result.Title);
        Assert.Equal("OverrideCo", result.Company);
        Assert.Equal("Remote", result.Location);
        Assert.Equal(JobWorkMode.Remote, result.WorkMode);
        Assert.Equal("Senior", result.Seniority);
    }

    [Fact]
    public void Normalize_rejects_empty_source_text()
    {
        Assert.Throws<ArgumentException>(() => new JobNormalizer().Normalize(new AddJobRequest(" ")));
    }
}
