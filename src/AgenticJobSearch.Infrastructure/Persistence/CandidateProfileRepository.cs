using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class CandidateProfileRepository(JobSearchDbContext dbContext, ICurrentUser currentUser) : ICandidateProfileRepository
{
    public async Task<CandidateProfile> GetDefaultAsync(CancellationToken cancellationToken)
    {
        var ownerId = currentUser.Id ?? throw new InvalidOperationException("Sign in first.");
        var profile = await dbContext.CandidateProfiles
            .Where(item => item.OwnerId == ownerId)
            .Include(item => item.Evidence)
            .OrderBy(item => item.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        profile = new CandidateProfile { Id = Guid.NewGuid(), OwnerId = ownerId };
        dbContext.CandidateProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }
}
