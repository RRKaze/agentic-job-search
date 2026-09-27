using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class HostingApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Health_endpoints_report_liveness_and_database_readiness()
    {
        var live = await fixture.Client.GetAsync("/api/health/live");
        var ready = await fixture.Client.GetAsync("/api/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task Request_correlation_identifier_is_returned_to_the_client()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health/live");
        request.Headers.Add("X-Correlation-ID", "daily-pilot-check-123");

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("daily-pilot-check-123", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task Forwarded_https_is_applied_before_the_authentication_cookie_is_created()
    {
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/login")
        {
            Content = JsonContent.Create(new
            {
                email = "owner@example.test",
                password = "Fictional test passphrase 2026"
            })
        };
        request.Headers.Add("X-Agentic-Request", "1");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
            value.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Unknown_api_routes_remain_not_found_in_the_single_page_host()
    {
        var response = await fixture.Client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Authentication_keys_are_persisted_in_the_database()
    {
        Assert.True(await fixture.CountDataProtectionKeysAsync() > 0);
    }

    [Fact]
    public void Production_configuration_rejects_missing_hosting_secrets()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfiguration.Validate(
            isProduction: true,
            configuration,
            new HostingOptions(),
            new AccountRegistrationOptions()));

        Assert.Contains("ConnectionStrings__JobSearch", exception.Message);
        Assert.Contains("Hosting__DataProtectionCertificateBase64", exception.Message);
        Assert.Contains("Accounts__RegistrationBootstrapToken", exception.Message);
    }
}
