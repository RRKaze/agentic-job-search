using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticJobSearch.Infrastructure.Persistence.Configurations;

public sealed class ImportCheckpointConfiguration : IEntityTypeConfiguration<ImportCheckpoint>
{
    public void Configure(EntityTypeBuilder<ImportCheckpoint> entity) => entity.HasKey(x => x.Source);
}

public sealed class WorkflowChangeConfiguration : IEntityTypeConfiguration<WorkflowChange>
{
    public void Configure(EntityTypeBuilder<WorkflowChange> entity)
    {
        entity.HasIndex(x => new { x.OwnerId, x.JobId, x.ChangedAt });
        entity.Property(x => x.PreviousStatus).HasMaxLength(40);
        entity.Property(x => x.CurrentStatus).HasMaxLength(40);
        entity.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}
