using AgenticJobSearch.Application.Abstractions;

namespace AgenticJobSearch.Application.Candidates;

public sealed class GetCandidateProfileHandler(ICandidateProfileRepository candidateProfiles)
{
    public async Task<CandidateProfileDto> HandleAsync(CancellationToken cancellationToken)
    {
        var profile = await candidateProfiles.GetDefaultAsync(cancellationToken);

        return new CandidateProfileDto(
            profile.Id,
            profile.DisplayName,
            profile.TargetLevel,
            profile.TargetRoleFamilies,
            profile.RequiresSponsorship,
            profile.Evidence.Select(item => new CandidateEvidenceDto(
                item.Category,
                item.Statement,
                item.VerificationStatus,
                item.Source)).ToList());
    }
}
