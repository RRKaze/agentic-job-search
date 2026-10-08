namespace AgenticJobSearch.Domain;

public sealed class PasswordResetToken
{
    public string TokenHash { get; set; } = "";
    public Guid AccountId { get; set; }
    public long SessionVersion { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}

public enum RecoveryMailKind { ResetLink, PasswordChanged }

// Durable delivery state. Prepared messages are encrypted with the application's data-protection keys.
public sealed class RecoveryEmail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public RecoveryMailKind Kind { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int Attempts { get; set; }
    public string? ProtectedMessage { get; set; }
    public string? TokenHash { get; set; }
}
