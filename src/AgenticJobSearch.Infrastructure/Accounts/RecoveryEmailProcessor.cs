using System.Security.Cryptography;
using System.Text.Json;
using AgenticJobSearch.Application.Accounts;
using AgenticJobSearch.Domain;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticJobSearch.Infrastructure.Accounts;

public sealed class RecoveryEmailProcessor(JobSearchDbContext db, IDataProtectionProvider protection,
    IRecoveryEmailSender sender, IOptions<PasswordRecoveryOptions> options, TimeProvider clock,
    ILogger<RecoveryEmailProcessor> logger)
{
    private readonly IDataProtector protector = protection.CreateProtector("AgenticJobSearch.PasswordRecovery.Email.v1");

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return false;
        var now = clock.GetUtcNow();
        Guid id;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var email = await db.RecoveryEmails.FromSqlInterpolated($"""
                SELECT * FROM "RecoveryEmails" WHERE "CompletedAt" IS NULL AND "NextAttemptAt" <= {now}
                ORDER BY "NextAttemptAt", "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
                """).SingleOrDefaultAsync(cancellationToken);
            if (email is null) return false;
            id = email.Id;
            if (email.ExpiresAt <= now) Complete(email, now);
            else if (email.ProtectedMessage is null)
            {
                var account = await db.UserAccounts.FromSqlInterpolated($"SELECT * FROM \"UserAccounts\" WHERE \"Email\" = {email.Email} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (account is null || (email.Kind == RecoveryMailKind.ResetLink && account.PasswordChangedAt >= email.CreatedAt))
                    Complete(email, now);
                else
                {
                    RecoveryEmailMessage message;
                    if (email.Kind == RecoveryMailKind.ResetLink)
                    {
                        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                        email.TokenHash = PasswordRecoveryService.HashToken(token);
                        db.PasswordResetTokens.Add(new PasswordResetToken { TokenHash = email.TokenHash, AccountId = account.Id,
                            SessionVersion = account.SessionVersion, ExpiresAt = email.ExpiresAt });
                        // A fragment never reaches HTTP access logs or referrers. The UI removes it immediately.
                        var url = options.Value.PublicBaseUrl.TrimEnd('/') + "/reset-password#token=" + token;
                        message = new(options.Value.From, email.Email, "Reset your Agentic Job Search password",
                            $"Open this link to choose a new password:\n\n{url}\n\nThis link expires 30 minutes after your request and can be used once. If you did not request this, ignore this email. Your password has not changed.");
                    }
                    else message = new(options.Value.From, email.Email, "Your Agentic Job Search password was changed",
                        "Your password was changed and existing sessions were invalidated. Your saved job-search data is unchanged. If this was not you, request a new password reset immediately from the application's sign-in page.");
                    email.ProtectedMessage = protector.Protect(JsonSerializer.Serialize(message));
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        db.ChangeTracker.Clear();
        // Prepared content is committed before sending. A crash after acceptance retries the same idempotency key and body.
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var email = await db.RecoveryEmails.FromSqlInterpolated($"SELECT * FROM \"RecoveryEmails\" WHERE \"Id\" = {id} FOR UPDATE SKIP LOCKED")
                .SingleOrDefaultAsync(cancellationToken);
            if (email is null || email.CompletedAt is not null || email.NextAttemptAt > clock.GetUtcNow()) return true;
            now = clock.GetUtcNow();
            if (email.ExpiresAt <= now || (email.TokenHash is not null &&
                !await db.PasswordResetTokens.AnyAsync(x => x.TokenHash == email.TokenHash && x.UsedAt == null && x.ExpiresAt > now, cancellationToken)))
                Complete(email, now);
            else
            {
                email.Attempts++;
                try
                {
                    var message = JsonSerializer.Deserialize<RecoveryEmailMessage>(protector.Unprotect(email.ProtectedMessage!))!;
                    await sender.SendAsync(email.Id, message, cancellationToken);
                    Complete(email, clock.GetUtcNow());
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested &&
                    exception is HttpRequestException or TaskCanceledException or CryptographicException or JsonException)
                {
                    logger.LogWarning("Recovery email delivery {DeliveryId} failed on attempt {Attempt}; sensitive details suppressed.", email.Id, email.Attempts);
                    if (email.Attempts >= 5) Complete(email, clock.GetUtcNow());
                    else email.NextAttemptAt = clock.GetUtcNow().AddSeconds(Math.Min(900, 30 * Math.Pow(2, email.Attempts - 1)));
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        return true;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await db.PasswordResetTokens.Where(x => x.ExpiresAt < now.AddDays(-1)).ExecuteDeleteAsync(cancellationToken);
        await db.RecoveryEmails.Where(x => x.CreatedAt < now.AddDays(-7)).ExecuteDeleteAsync(cancellationToken);
    }
    private static void Complete(RecoveryEmail email, DateTimeOffset now)
    {
        email.CompletedAt = now;
        email.ProtectedMessage = null;
    }
}
