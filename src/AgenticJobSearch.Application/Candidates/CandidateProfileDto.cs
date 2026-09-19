using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Candidates;

public sealed record CandidateProfileDto(
    Guid Id,
    string DisplayName,
    string TargetLevel,
    string TargetRoleFamilies,
    bool RequiresSponsorship,
    IReadOnlyList<CandidateEvidenceDto> Evidence);

public sealed record CandidateEvidenceDto(
    string Category,
    string Statement,
    EvidenceVerificationStatus VerificationStatus,
    string Source);
