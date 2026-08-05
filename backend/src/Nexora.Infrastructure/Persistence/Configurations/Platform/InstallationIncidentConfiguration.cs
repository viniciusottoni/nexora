using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class InstallationIncidentConfiguration : IEntityTypeConfiguration<InstallationIncident>
{
    public void Configure(EntityTypeBuilder<InstallationIncident> builder)
    {
        builder.ToTable("installation_incident");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.InstallationId).HasColumnName("installation_id");
        builder.Property(i => i.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Cause).HasColumnName("cause");
        builder.Property(i => i.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz");
        builder.Property(i => i.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamptz");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(i => new { i.TenantId, i.InstallationId }).HasDatabaseName("idx_installation_incident_installation");
        builder.HasIndex(i => new { i.InstallationId, i.ResolvedAt })
            .HasDatabaseName("idx_installation_incident_open")
            .HasFilter("resolved_at IS NULL");

        builder.HasOne<EdgeInstallation>().WithMany().HasForeignKey(i => i.InstallationId);
    }
}
