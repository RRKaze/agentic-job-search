namespace AgenticJobSearch.Application.Accounts;

public sealed record RegisterAccountRequest(string? DisplayName, string? Email, string? Password, string? BootstrapToken = null);
public sealed record LoginAccountRequest(string? Email, string? Password);
public sealed record UpdateAccountProfileRequest(string? DisplayName, string? CareerStage);

public sealed record AccountProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? CareerStage,
    DateTimeOffset CreatedAt,
    [property: System.Text.Json.Serialization.JsonIgnore] long SessionVersion = 0);

public sealed class AccountFailure(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
