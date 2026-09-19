using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class AddJobValidationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Empty_job_description_returns_validation_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/jobs", new
        {
            sourceText = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("sourceText", out _));
    }

    [Fact]
    public async Task Oversized_job_description_returns_validation_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/jobs", new
        {
            sourceText = new string('x', 20_001)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("sourceText", out _));
    }

    [Fact]
    public async Task Invalid_source_url_returns_validation_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/jobs", new
        {
            sourceText = "A valid job description.",
            sourceUrl = "not-a-url"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("sourceUrl", out _));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("company")]
    [InlineData("location")]
    public async Task Oversized_summary_field_returns_validation_problem(string field)
    {
        var request = new Dictionary<string, string>
        {
            ["sourceText"] = "A valid job description.",
            [field] = new string('x', 301)
        };

        var response = await fixture.Client.PostAsJsonAsync("/api/jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Malformed_json_returns_problem_details()
    {
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await fixture.Client.PostAsync("/api/jobs", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
