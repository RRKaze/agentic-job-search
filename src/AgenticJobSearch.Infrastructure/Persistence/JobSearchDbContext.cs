using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgenticJobSearch.Infrastructure.Persistence;

public sealed class JobSearchDbContext(DbContextOptions<JobSearchDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateEvidence> CandidateEvidence => Set<CandidateEvidence>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobEvaluation> JobEvaluations => Set<JobEvaluation>();
    public DbSet<JobEvaluationFactor> JobEvaluationFactors => Set<JobEvaluationFactor>();
    public DbSet<Domain.Application> Applications => Set<Domain.Application>();
    public DbSet<Feedback> Feedback => Set<Feedback>();

    public DbSet<ImportCheckpoint> ImportCheckpoints => Set<ImportCheckpoint>();
    public DbSet<TrackingChange> TrackingChanges => Set<TrackingChange>();
    public DbSet<WorkflowChange> WorkflowChanges => Set<WorkflowChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobSearchDbContext).Assembly);
    }
}
