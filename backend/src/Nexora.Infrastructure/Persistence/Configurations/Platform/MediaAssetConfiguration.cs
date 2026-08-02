using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_asset");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.OwnerType).HasColumnName("owner_type").HasMaxLength(32).IsRequired();
        builder.Property(m => m.OwnerId).HasColumnName("owner_id");
        builder.Property(m => m.Variant).HasColumnName("variant").HasMaxLength(16).IsRequired();
        builder.Property(m => m.Url).HasColumnName("url").IsRequired();
        builder.Property(m => m.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
        builder.Property(m => m.Width).HasColumnName("width");
        builder.Property(m => m.Height).HasColumnName("height");
        builder.Property(m => m.Bytes).HasColumnName("bytes");
        builder.Property(m => m.MimeType).HasColumnName("mime_type").HasMaxLength(64);
        builder.Property(m => m.BlurData).HasColumnName("blur_data");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(m => new { m.TenantId, m.OwnerType, m.OwnerId, m.Variant, m.ContentHash })
            .IsUnique()
            .HasDatabaseName("uq_media_asset");

        builder.HasIndex(m => new { m.TenantId, m.OwnerType, m.OwnerId }).HasDatabaseName("idx_media_asset_owner");
    }
}
