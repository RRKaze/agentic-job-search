using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class CandidateProfileRepository(JobSearchDbContext dbContext) : ICandidateProfileRepository
{
    public async Task<CandidateProfile> GetDefaultAsync(CancellationToken cancellationToken)
    {
        var profile = await dbContext.CandidateProfiles
            .Include(item => item.Evidence)
            .OrderBy(item => item.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        profile = SeedCandidateProfile.Create();
        dbContext.CandidateProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }
}
