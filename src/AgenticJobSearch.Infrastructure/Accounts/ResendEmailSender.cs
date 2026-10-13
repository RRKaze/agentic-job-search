using System.Net.Http.Headers;
using System.Net.Http.Json;
using AgenticJobSearch.Application.Accounts;
using Microsoft.Extensions.Options;

namespace AgenticJobSearch.Infrastructure.Accounts;

public sealed class ResendEmailSender(HttpClient client, IOptions<PasswordRecoveryOptions> options) : IRecoveryEmailSender
{
    public async Task SendAsync(Guid deliveryId, RecoveryEmailMessage message, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ResendApiKey);
        request.Headers.Add("Idempotency-Key", $"password-recovery/{deliveryId}");
        request.Content = JsonContent.Create(new { from = message.From, to = new[] { message.To }, subject = message.Subject, text = message.Text });
        using var response = await client.SendAsync(request, cancellationToken);
        // Never include the response body, recipient, token, or provider credentials in errors/logs.
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Recovery email provider rejected delivery.", null, response.StatusCode);
    }
}
