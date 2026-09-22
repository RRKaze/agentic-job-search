using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class JobRepository(JobSearchDbContext dbContext, ICurrentUser currentUser) : IJobRepository
{
    public async Task<Job> AddAsync(Job job, CancellationToken cancellationToken)
    {
        job.OwnerId = currentUser.Id ?? throw new InvalidOperationException("Sign in first.");
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<IReadOnlyList<Job>> ListRecentAsync(int count, CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .Where(job => currentUser.Id != null && (job.OwnerId == currentUser.Id || (job.OwnerId == null && currentUser.CanAccessLegacyWorkspace)))
            .AsNoTracking()
            .Include(job => job.Evaluation)
            .ThenInclude(evaluation => evaluation!.Factors)
            .Include(job => job.Application)
            .OrderByDescending(job => job.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
