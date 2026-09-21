using System.Net.Http.Headers;
using System.Text.Json;
using AgenticJobSearch.Application.Imports;
using Microsoft.Extensions.Configuration;
namespace AgenticJobSearch.Infrastructure.Imports;
public sealed class GitHubHeadVerifier(IConfiguration configuration) : ISourceHeadVerifier
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
    public async Task<string> GetHeadAsync(CancellationToken cancellationToken)
    {
        var token = configuration["Imports:GitHubReadToken"];
        if (string.IsNullOrWhiteSpace(token)) throw new ImportFailure(503, "SOURCE_VERIFICATION_UNAVAILABLE");
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/RRKaze/job-search/commits/main");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.UserAgent.ParseAdd("AgenticJobSearch/1.0");
        try
        {
            using var response = await Client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ImportFailure(503, "SOURCE_VERIFICATION_UNAVAILABLE");
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var sha = document.RootElement.GetProperty("sha").GetString();
            if (sha is null || !System.Text.RegularExpressions.Regex.IsMatch(sha, "^[0-9a-f]{40}$"))
                throw new ImportFailure(503, "SOURCE_VERIFICATION_UNAVAILABLE");
            return sha;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        { throw new ImportFailure(503, "SOURCE_VERIFICATION_UNAVAILABLE"); }
    }
}
