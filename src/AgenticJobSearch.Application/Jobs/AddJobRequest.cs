namespace AgenticJobSearch.Application.Jobs;

public sealed record AddJobRequest(
    string SourceText,
    string? SourceUrl = null,
    string? Title = null,
    string? Company = null,
    string? Location = null);
