using AgenticJobSearch.Application.Abstractions;

namespace AgenticJobSearch.Application.Candidates;

public sealed class GetCandidateProfileHandler(ICandidateProfileRepository candidateProfiles)
{
    public async Task<CandidateProfileDto> HandleAsync(CancellationToken cancellationToken) =>
        (await candidateProfiles.GetDefaultAsync(cancellationToken)).ToDto();
}
