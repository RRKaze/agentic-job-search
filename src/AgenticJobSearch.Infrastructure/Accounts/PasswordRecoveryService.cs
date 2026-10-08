using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Application.Accounts;
using AgenticJobSearch.Domain;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgenticJobSearch.Infrastructure.Accounts;

public sealed class PasswordRecoveryService(JobSearchDbContext db, IPasswordService passwords,
    IOptions<PasswordRecoveryOptions> options, TimeProvider clock) : IPasswordRecoveryService
{
    public async Task RequestAsync(string? email, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        email = email?.Trim().ToLowerInvariant() ?? "";
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email) return;
        var now = clock.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize the small private-beta request budget across instances, including unknown emails.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(684209731)", cancellationToken);
        var requests = db.RecoveryEmails.Where(x => x.Kind == RecoveryMailKind.ResetLink);
        if (await requests.CountAsync(x => x.CreatedAt > now.AddDays(-1), cancellationToken) >= 40 ||
            await requests.CountAsync(x => x.Email == email && x.CreatedAt > now.AddHours(-1), cancellationToken) >= 3 ||
            await requests.AnyAsync(x => x.Email == email && x.CreatedAt > now.AddMinutes(-1), cancellationToken)) return;
        // Account lookup and provider delivery happen off the request path to avoid account enumeration.
        db.RecoveryEmails.Add(new RecoveryEmail { Email = email, CreatedAt = now, NextAttemptAt = now, ExpiresAt = now.AddMinutes(30) });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (request.Password is null || request.Password.Length is < 12 or > 128 || request.Password != request.ConfirmPassword)
            throw new AccountFailure(400, "Enter matching passwords between 12 and 128 characters.");
        if (request.Token is null || request.Token.Length != 64 || !request.Token.All(Uri.IsHexDigit)) throw InvalidLink();
        var hash = HashToken(request.Token);
        var accountId = await db.PasswordResetTokens.AsNoTracking().Where(x => x.TokenHash == hash)
            .Select(x => (Guid?)x.AccountId).SingleOrDefaultAsync(cancellationToken);
        if (accountId is null) throw InvalidLink();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Every reset/issuance for this account takes the same lock. Different valid links cannot both win.
        var account = await db.UserAccounts.FromSqlInterpolated($"SELECT * FROM \"UserAccounts\" WHERE \"Id\" = {accountId.Value} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        var token = await db.PasswordResetTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        var now = clock.GetUtcNow();
        if (account is null || token is null || token.UsedAt is not null || token.ExpiresAt <= now || token.SessionVersion != account.SessionVersion)
            throw InvalidLink();
        account.PasswordHash = passwords.Hash(account, request.Password);
        account.SessionVersion++;
        account.PasswordChangedAt = now;
        await db.PasswordResetTokens.Where(x => x.AccountId == account.Id && x.UsedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAt, now), cancellationToken);
        db.RecoveryEmails.Add(new RecoveryEmail { Email = account.Email, Kind = RecoveryMailKind.PasswordChanged,
            CreatedAt = now, NextAttemptAt = now, ExpiresAt = now.AddHours(12) });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private void EnsureEnabled()
    {
        if (!options.Value.Enabled) throw new AccountFailure(503, "Password recovery is not available yet. Please try again later.");
    }
    private static AccountFailure InvalidLink() => new(400, "This reset link is invalid or has expired. Request a new link.");
}
