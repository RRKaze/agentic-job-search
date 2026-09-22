using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class JobRepository(JobSearchDbContext dbContext) : IJobRepository
{
    public async Task<Job> AddAsync(Job job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<IReadOnlyList<Job>> ListRecentAsync(int count, CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .AsNoTracking()
            .Include(job => job.Evaluation)
            .ThenInclude(evaluation => evaluation!.Factors)
            .Include(job => job.Application)
            .OrderByDescending(job => job.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
