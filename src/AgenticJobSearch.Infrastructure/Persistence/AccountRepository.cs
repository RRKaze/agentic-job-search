using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class AccountRepository(JobSearchDbContext dbContext) : IAccountRepository
{
    public async Task<bool> AddAsync(UserAccount account, CandidateProfile profile, CancellationToken cancellationToken)
    {
        dbContext.UserAccounts.Add(account);
        dbContext.CandidateProfiles.Add(profile);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.UserAccounts.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.UserAccounts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdatePasswordHashAsync(UserAccount account, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task UpdateProfileAsync(UserAccount account, CancellationToken cancellationToken)
    {
        var profile = await dbContext.CandidateProfiles.SingleAsync(x => x.OwnerId == account.Id, cancellationToken);
        profile.DisplayName = account.DisplayName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
