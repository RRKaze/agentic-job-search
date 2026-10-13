using AgenticJobSearch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticJobSearch.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> entity)
    {
        entity.HasKey(x => x.TokenHash);
        entity.Property(x => x.TokenHash).HasMaxLength(64);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(x => x.ExpiresAt);
    }
}
public sealed class RecoveryEmailConfiguration : IEntityTypeConfiguration<RecoveryEmail>
{
    public void Configure(EntityTypeBuilder<RecoveryEmail> entity)
    {
        entity.Property(x => x.Email).HasMaxLength(254);
        entity.Property(x => x.TokenHash).HasMaxLength(64);
        entity.HasIndex(x => new { x.CompletedAt, x.NextAttemptAt });
        entity.HasIndex(x => new { x.Email, x.CreatedAt });
        entity.HasIndex(x => x.CreatedAt);
    }
}
