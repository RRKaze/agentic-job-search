using System.Net;
using System.Text.Json;
using AgenticJobSearch.Application.Accounts;
using AgenticJobSearch.Infrastructure.Accounts;
using Microsoft.Extensions.Options;

namespace AgenticJobSearch.Api.IntegrationTests;

public sealed class RecoveryEmailSenderTests
{
    [Fact]
    public async Task Provider_request_uses_stable_idempotency_and_does_not_expose_error_body()
    {
        var id = Guid.NewGuid();
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var sender = new ResendEmailSender(client, Options.Create(new PasswordRecoveryOptions { ResendApiKey = "fictional-key" }));
        var message = new RecoveryEmailMessage("accounts@example.test", "fictional@example.test", "Reset", "Fictional link");
        await sender.SendAsync(id, message, default);
        handler.Status = HttpStatusCode.TooManyRequests;
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => sender.SendAsync(id, message, default));
        Assert.DoesNotContain("sensitive-provider-body", error.ToString());
        Assert.Equal(handler.Requests[0], handler.Requests[1]);
        Assert.Equal($"password-recovery/{id}", handler.Requests[0].Key);
        Assert.Equal("Bearer fictional-key", handler.Requests[0].Authorization);
        var json = JsonDocument.Parse(handler.Requests[0].Body).RootElement;
        Assert.Equal(message.To, json.GetProperty("to")[0].GetString());
        Assert.Equal(message.Text, json.GetProperty("text").GetString());
    }

    [Theory]
    [InlineData("https://app.example.test", true)]
    [InlineData("http://app.example.test", false)]
    [InlineData("https://app.example.test/path", false)]
    [InlineData("https://app.example.test/?q=test", false)]
    [InlineData("https://user@app.example.test", false)]
    [InlineData("", false)]
    public void Enabled_recovery_requires_a_fixed_https_origin(string url, bool expected)
    {
        var options = new PasswordRecoveryOptions { Enabled = true, PublicBaseUrl = url, From = "Test <accounts@example.test>", ResendApiKey = "fictional-key" };
        Assert.Equal(expected, options.IsValid());
        options.Enabled = false; Assert.True(options.IsValid());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public List<(string Key, string Authorization, string Body)> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.resend.com/emails", request.RequestUri!.AbsoluteUri);
            Requests.Add((request.Headers.GetValues("Idempotency-Key").Single(), request.Headers.Authorization!.ToString(), await request.Content!.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(Status) { Content = new StringContent("sensitive-provider-body") };
        }
    }
}
