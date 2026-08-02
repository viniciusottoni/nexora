using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class AuthAttemptConfiguration : IEntityTypeConfiguration<AuthAttempt>
{
    public void Configure(EntityTypeBuilder<AuthAttempt> builder)
    {
        builder.ToTable("auth_attempt");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.DeviceId).HasColumnName("device_id");
        builder.Property(a => a.FailedAttempts).HasColumnName("failed_attempts").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(a => a.BlockedUntil).HasColumnName("blocked_until").HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(a => a.DeviceId).IsUnique().HasDatabaseName("uq_auth_attempt_device");
        builder.HasIndex(a => new { a.TenantId, a.BlockedUntil }).HasDatabaseName("idx_auth_attempt_tenant_blocked");

        builder.HasOne(a => a.Device).WithOne(d => d.AuthAttempt)
            .HasForeignKey<AuthAttempt>(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
