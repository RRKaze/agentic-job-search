namespace AgenticJobSearch.Domain;

public sealed class CandidateEvidence
{
    public Guid Id { get; set; }
    public Guid CandidateProfileId { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public EvidenceVerificationStatus VerificationStatus { get; set; } = EvidenceVerificationStatus.Verified;
    public string Source { get; set; } = string.Empty;
}
