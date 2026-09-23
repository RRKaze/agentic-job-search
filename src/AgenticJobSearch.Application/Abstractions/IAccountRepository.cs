using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Abstractions;

public interface IAccountRepository
{
    Task<bool> AddAsync(UserAccount account, CandidateProfile profile, CancellationToken cancellationToken);
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdatePasswordHashAsync(UserAccount account, CancellationToken cancellationToken);
    Task UpdateProfileAsync(UserAccount account, CancellationToken cancellationToken);
}

public interface IPasswordService
{
    string Hash(UserAccount account, string password);
    PasswordCheckResult Verify(UserAccount? account, string password);
}

public enum PasswordCheckResult
{
    Failed,
    Success,
    RehashNeeded
}
