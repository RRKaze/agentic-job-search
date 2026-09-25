using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticJobSearch.Infrastructure.Persistence.Configurations;

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<Domain.Application>
{
    public void Configure(EntityTypeBuilder<Domain.Application> entity)
    {
        entity.HasIndex(x => new { x.SourceRepository, x.ExternalId }).IsUnique();
        entity.Property(x => x.ImportedRecord).HasColumnType("jsonb");
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(40);
        entity.HasMany(x => x.Feedback).WithOne(x => x.Application)
            .HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> entity)
    {
        entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        entity.Property(x => x.Notes).HasMaxLength(3000);
    }
}
