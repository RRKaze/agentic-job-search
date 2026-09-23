using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;
using Microsoft.AspNetCore.Identity;

namespace AgenticJobSearch.Infrastructure.Security;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<UserAccount> hasher = new();
    private readonly string dummyHash;

    public PasswordService()
    {
        dummyHash = hasher.HashPassword(new UserAccount(), "Fictional timing comparison only");
    }

    public string Hash(UserAccount account, string password) => hasher.HashPassword(account, password);

    public PasswordCheckResult Verify(UserAccount? account, string password)
    {
        var result = hasher.VerifyHashedPassword(account ?? new UserAccount(), account?.PasswordHash ?? dummyHash, password);
        return result switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.RehashNeeded,
            _ => PasswordCheckResult.Failed
        };
    }
}
