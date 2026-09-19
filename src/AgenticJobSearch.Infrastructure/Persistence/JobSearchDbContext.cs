using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class JobSearchDbContext(DbContextOptions<JobSearchDbContext> options) : DbContext(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateEvidence> CandidateEvidence => Set<CandidateEvidence>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobEvaluation> JobEvaluations => Set<JobEvaluation>();
    public DbSet<JobEvaluationFactor> JobEvaluationFactors => Set<JobEvaluationFactor>();
    public DbSet<Domain.Application> Applications => Set<Domain.Application>();
    public DbSet<Feedback> Feedback => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.Property(item => item.DisplayName).HasMaxLength(200);
            entity.Property(item => item.TargetLevel).HasMaxLength(120);
            entity.Property(item => item.TargetRoleFamilies).HasMaxLength(500);
            entity.HasMany(item => item.Evidence)
                .WithOne(item => item.CandidateProfile)
                .HasForeignKey(item => item.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateEvidence>(entity =>
        {
            entity.Property(item => item.Category).HasMaxLength(100);
            entity.Property(item => item.Statement).HasMaxLength(1200);
            entity.Property(item => item.Source).HasMaxLength(300);
            entity.Property(item => item.VerificationStatus).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.Property(item => item.SourceText).HasMaxLength(20000);
            entity.Property(item => item.SourceUrl).HasMaxLength(1000);
            entity.Property(item => item.Title).HasMaxLength(300);
            entity.Property(item => item.Company).HasMaxLength(300);
            entity.Property(item => item.Location).HasMaxLength(300);
            entity.Property(item => item.Seniority).HasMaxLength(80);
            entity.Property(item => item.WorkMode).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.LifecycleState).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(item => item.Evaluation)
                .WithOne(item => item.Job)
                .HasForeignKey<JobEvaluation>(item => item.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Application)
                .WithOne(item => item.Job)
                .HasForeignKey<Domain.Application>(item => item.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobEvaluation>(entity =>
        {
            entity.Property(item => item.Eligibility).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.Recommendation).HasMaxLength(120);
            entity.Property(item => item.Explanation).HasMaxLength(3000);
            entity.HasMany(item => item.Factors)
                .WithOne(item => item.JobEvaluation)
                .HasForeignKey(item => item.JobEvaluationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobEvaluationFactor>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Rationale).HasMaxLength(1000);
        });

        modelBuilder.Entity<Domain.Application>(entity =>
        {
            entity.Property(item => item.State).HasConversion<string>().HasMaxLength(40);
            entity.HasMany(item => item.Feedback)
                .WithOne(item => item.Application)
                .HasForeignKey(item => item.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.Property(item => item.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.Notes).HasMaxLength(3000);
        });
    }
}
