using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Abstractions;

public interface ICandidateProfileRepository
{
    Task<CandidateProfile> GetDefaultAsync(CancellationToken cancellationToken);
}
