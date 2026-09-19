using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Abstractions;

public interface IJobRepository
{
    Task<Job> AddAsync(Job job, CancellationToken cancellationToken);
    Task<IReadOnlyList<Job>> ListRecentAsync(int count, CancellationToken cancellationToken);
}
