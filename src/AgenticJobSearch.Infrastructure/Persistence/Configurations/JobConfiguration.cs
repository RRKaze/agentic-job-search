using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticJobSearch.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> entity)
    {
        entity.HasIndex(x => x.OwnerId);
        entity.HasIndex(x => new { x.SourceRepository, x.ExternalId }).IsUnique();
        entity.Property(x => x.ImportedRecord).HasColumnType("jsonb");
        entity.Property(x => x.SourceText).HasMaxLength(20000);
        entity.Property(x => x.SourceUrl).HasMaxLength(2048);
        entity.Property(x => x.Title).HasMaxLength(500);
        entity.Property(x => x.Company).HasMaxLength(500);
        entity.Property(x => x.Location).HasMaxLength(500);
        entity.Property(x => x.Seniority).HasMaxLength(80);
        entity.Property(x => x.WorkMode).HasConversion<string>().HasMaxLength(40);
        entity.Property(x => x.LifecycleState).HasConversion<string>().HasMaxLength(40);
        entity.HasOne(x => x.Evaluation).WithOne(x => x.Job)
            .HasForeignKey<JobEvaluation>(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Application).WithOne(x => x.Job)
            .HasForeignKey<Domain.Application>(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class JobEvaluationConfiguration : IEntityTypeConfiguration<JobEvaluation>
{
    public void Configure(EntityTypeBuilder<JobEvaluation> entity)
    {
        entity.Property(x => x.Eligibility).HasConversion<string>().HasMaxLength(40);
        entity.Property(x => x.Recommendation).HasMaxLength(120);
        entity.Property(x => x.Explanation).HasMaxLength(3000);
        entity.HasMany(x => x.Factors).WithOne(x => x.JobEvaluation)
            .HasForeignKey(x => x.JobEvaluationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class JobEvaluationFactorConfiguration : IEntityTypeConfiguration<JobEvaluationFactor>
{
    public void Configure(EntityTypeBuilder<JobEvaluationFactor> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(120);
        entity.Property(x => x.Rationale).HasMaxLength(1000);
    }
}
