namespace AgenticJobSearch.Domain;

public sealed class CandidateProfile
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;
    public string TargetRoleFamilies { get; set; } = string.Empty;
    public bool RequiresSponsorship { get; set; }
    public List<CandidateEvidence> Evidence { get; set; } = [];
}
