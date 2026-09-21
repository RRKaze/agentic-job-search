using System.Text.Json;
using System.Text.Json.Serialization;
namespace AgenticJobSearch.Application.Imports;

public sealed record ImportSource(string Repository, string Branch, string CommitSha, string? ExpectedPreviousCommit);
public sealed record ImportRequest(string SchemaVersion, bool DryRun, ImportSource Source,
    List<Dictionary<string, string?>> Jobs, List<Dictionary<string, string?>> Applications);
public sealed record ImportIssue(string Entity, int Row, string Field, string Code);
public sealed record ImportCounts(int Inserted, int Updated, int Unchanged);
public sealed class ImportFailure(int status, string code, IEnumerable<ImportIssue>? details = null) : Exception(code)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public IReadOnlyList<ImportIssue> Details { get; } = details?.ToList() ?? [];
}
public interface ISourceHeadVerifier
{
    Task<string> GetHeadAsync(CancellationToken cancellationToken);
}
public static class ImportJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
}
