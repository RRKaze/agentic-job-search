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

    public async Task<Job?> GetOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await OwnedJobs()
            .Include(job => job.Evaluation)
            .ThenInclude(evaluation => evaluation!.Factors)
            .Include(job => job.Application)
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task SaveWorkflowChangeAsync(Job job, WorkflowChange? change, bool applicationCreated, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not Guid ownerId) throw new InvalidOperationException("Sign in first.");
        if (job.OwnerId is null)
        {
            if (!currentUser.CanAccessLegacyWorkspace) throw new InvalidOperationException("This job does not belong to the current account.");
            job.OwnerId = ownerId;
        }
        if (applicationCreated && job.Application is not null)
            dbContext.Entry(job.Application).State = EntityState.Added;
        if (change is not null)
        {
            change.OwnerId = ownerId;
            dbContext.WorkflowChanges.Add(change);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowChange>> ListWorkflowChangesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not Guid ownerId) return [];
        if (!await OwnedJobs().AnyAsync(job => job.Id == jobId, cancellationToken)) return [];
        return await dbContext.WorkflowChanges.AsNoTracking()
            .Where(change => change.OwnerId == ownerId && change.JobId == jobId)
            .OrderByDescending(change => change.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> ListRecentAsync(int count, CancellationToken cancellationToken)
    {
        return await OwnedJobs()
            .AsNoTracking()
            .Include(job => job.Evaluation)
            .ThenInclude(evaluation => evaluation!.Factors)
            .Include(job => job.Application)
            .OrderByDescending(job => job.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Job> OwnedJobs() => dbContext.Jobs
        .Where(job => currentUser.Id != null && (job.OwnerId == currentUser.Id || (job.OwnerId == null && currentUser.CanAccessLegacyWorkspace)));
}
