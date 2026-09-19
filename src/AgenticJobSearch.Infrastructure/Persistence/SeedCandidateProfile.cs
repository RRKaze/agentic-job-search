using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Infrastructure.Persistence;

internal static class SeedCandidateProfile
{
    public static CandidateProfile Create()
    {
        var profileId = Guid.Parse("c7616e06-8e9a-43dc-b462-cbf3b1641091");
        var source = "Fictional public demo fixture";

        return new CandidateProfile
        {
            Id = profileId,
            DisplayName = "Default Senior SWE candidate",
            TargetLevel = "Senior Software Engineer",
            TargetRoleFamilies = "Backend, Platform, Identity/Authentication, Distributed Systems",
            RequiresSponsorship = false,
            Evidence =
            [
                Evidence(profileId, "Backend", "Built backend services with C#, .NET / ASP.NET Core, REST APIs, and SQL.", source),
                Evidence(profileId, "Frontend", "Built user-facing features with React, TypeScript, and JavaScript.", source),
                Evidence(profileId, "Identity", "Contributed to authentication and authorization workflows.", source),
                Evidence(profileId, "Distributed systems", "Implemented producer/consumer processing with retry handling and data validation.", source),
                Evidence(profileId, "Production", "Diagnosed production issues and participated in incident response and customer support.", source),
                Evidence(profileId, "Product collaboration", "Collaborated with product and design partners on requirements, dependencies, and tradeoffs.", source)
            ]
        };
    }

    private static CandidateEvidence Evidence(Guid profileId, string category, string statement, string source)
    {
        return new CandidateEvidence
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profileId,
            Category = category,
            Statement = statement,
            VerificationStatus = EvidenceVerificationStatus.Verified,
            Source = source
        };
    }
}
