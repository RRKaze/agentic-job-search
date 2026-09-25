using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Abstractions;

public interface IJobRepository
{
    Task<Job> AddAsync(Job job, CancellationToken cancellationToken);
    Task<Job?> GetOwnedAsync(Guid id, CancellationToken cancellationToken);
    Task SaveWorkflowChangeAsync(Job job, WorkflowChange? change, bool applicationCreated, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowChange>> ListWorkflowChangesAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Job>> ListRecentAsync(int count, CancellationToken cancellationToken);
}
