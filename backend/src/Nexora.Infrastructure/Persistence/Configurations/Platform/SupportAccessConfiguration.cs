using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class SupportAccessConfiguration : IEntityTypeConfiguration<SupportAccess>
{
    public void Configure(EntityTypeBuilder<SupportAccess> builder)
    {
        builder.ToTable("support_access");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.GrantedTo).HasColumnName("granted_to");
        builder.Property(s => s.Reason).HasColumnName("reason").IsRequired();
        builder.Property(s => s.DurationMinutes).HasColumnName("duration_minutes");
        builder.Property(s => s.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(s => s.GrantedAt).HasColumnName("granted_at").HasColumnType("timestamptz");
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(s => s.RevokedBy).HasColumnName("revoked_by");
        builder.Property(s => s.LastUsedAt).HasColumnName("last_used_at").HasColumnType("timestamptz");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(s => s.TenantId).HasDatabaseName("idx_support_access_tenant");
        builder.HasIndex(s => s.TokenHash).IsUnique().HasDatabaseName("uq_support_access_token_hash");
        builder.HasIndex(s => new { s.TenantId, s.ExpiresAt })
            .HasDatabaseName("idx_support_access_active")
            .HasFilter("revoked_at IS NULL");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId);
    }
}
