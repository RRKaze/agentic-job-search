using AgenticJobSearch.Application.Imports;
using Microsoft.Extensions.DependencyInjection.Extensions;

// This executable is test-only and is not included in the production Docker target.
var app = await ApiBootstrap.BuildAsync(args, builder =>
{
    if (!builder.Environment.IsEnvironment("ImportIntegration"))
        throw new InvalidOperationException("Test host requires the ImportIntegration environment.");
    var head = builder.Configuration["TestSource:Head"];
    if (head is null || !System.Text.RegularExpressions.Regex.IsMatch(head, "^[0-9a-f]{40}$"))
        throw new InvalidOperationException("Supply the fixture source commit in TestSource__Head.");
    builder.Services.RemoveAll<ISourceHeadVerifier>();
    builder.Services.AddSingleton<ISourceHeadVerifier>(new FixtureHeadVerifier(head));
});
await app.RunAsync();

sealed class FixtureHeadVerifier(string head) : ISourceHeadVerifier
{
    public Task<string> GetHeadAsync(CancellationToken cancellationToken) => Task.FromResult(head);
}
