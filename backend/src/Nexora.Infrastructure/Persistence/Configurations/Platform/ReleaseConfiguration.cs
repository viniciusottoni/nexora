using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>Catálogo de plataforma — sem <c>tenant_id</c>/RLS (mesma natureza de <see cref="Tenant"/>).</summary>
internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("release");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.Version).HasColumnName("version").HasMaxLength(20).IsRequired();
        builder.Property(r => r.RolloutPercent).HasColumnName("rollout_percent").HasDefaultValue(0);
        builder.Property(r => r.Notes).HasColumnName("notes");
        builder.Property(r => r.PublishedAt).HasColumnName("published_at").HasColumnType("timestamptz");
        builder.Property(r => r.PublishedBy).HasColumnName("published_by");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(r => r.Version).IsUnique().HasDatabaseName("uq_release_version");
    }
}
