using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticJobSearch.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> entity)
    {
        entity.HasIndex(x => x.Email).IsUnique();
        entity.Property(x => x.Email).HasMaxLength(254);
        entity.Property(x => x.DisplayName).HasMaxLength(200);
        entity.Property(x => x.CareerStage).HasMaxLength(30);
    }
}

public sealed class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
{
    public void Configure(EntityTypeBuilder<CandidateProfile> entity)
    {
        entity.HasIndex(x => x.OwnerId).IsUnique();
        entity.Property(x => x.DisplayName).HasMaxLength(200);
        entity.Property(x => x.Headline).HasMaxLength(300);
        entity.Property(x => x.ProfessionalSummary).HasMaxLength(3000);
        entity.Property(x => x.TargetLevel).HasMaxLength(120);
        entity.Property(x => x.TargetRoleFamilies).HasMaxLength(500);
        entity.Property(x => x.TargetIndustries).HasMaxLength(500);
        entity.Property(x => x.PreferredLocations).HasMaxLength(500);
        entity.Property(x => x.WorkModePreference).HasMaxLength(100);
        entity.Property(x => x.EmploymentTypePreference).HasMaxLength(100);
        entity.Property(x => x.Skills).HasMaxLength(3000);
        entity.Property(x => x.WorkAuthorization).HasMaxLength(200);
        entity.Property(x => x.LinkedInUrl).HasMaxLength(2048);
        entity.Property(x => x.PortfolioUrl).HasMaxLength(2048);
        entity.Property(x => x.ResumeText).HasMaxLength(30000);
        entity.Property(x => x.Institution).HasMaxLength(300);
        entity.Property(x => x.Degree).HasMaxLength(200);
        entity.Property(x => x.FieldOfStudy).HasMaxLength(200);
        entity.Property(x => x.RecentEmployer).HasMaxLength(300);
        entity.Property(x => x.RecentJobTitle).HasMaxLength(300);
        entity.HasMany(x => x.Evidence).WithOne(x => x.CandidateProfile)
            .HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CandidateEvidenceConfiguration : IEntityTypeConfiguration<CandidateEvidence>
{
    public void Configure(EntityTypeBuilder<CandidateEvidence> entity)
    {
        entity.Property(x => x.Category).HasMaxLength(100);
        entity.Property(x => x.Statement).HasMaxLength(1200);
        entity.Property(x => x.Source).HasMaxLength(300);
        entity.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(40);
    }
}
