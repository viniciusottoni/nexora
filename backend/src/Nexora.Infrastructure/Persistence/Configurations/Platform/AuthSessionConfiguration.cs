using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_session");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.DeviceId).HasColumnName("device_id");
        builder.Property(s => s.RefreshHash).HasColumnName("refresh_hash");
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(s => s.LastActiveAt).HasColumnName("last_active_at").HasColumnType("timestamptz");
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.TenantId, s.UserId }).HasDatabaseName("idx_auth_session_tenant_user");

        builder.HasOne(s => s.User).WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Device).WithMany(d => d.Sessions).HasForeignKey(s => s.DeviceId);
    }
}
