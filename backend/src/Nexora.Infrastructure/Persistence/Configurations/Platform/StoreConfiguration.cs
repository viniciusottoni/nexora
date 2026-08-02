using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("store");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.Timezone).HasColumnName("timezone").HasDefaultValue("America/Sao_Paulo");
        builder.Property(s => s.Address).HasColumnName("address").HasColumnType("jsonb");
        builder.Property(s => s.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(s => s.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(s => s.TenantId).HasDatabaseName("idx_store_tenant");

        // exatamente uma loja padrão por tenant — índice único parcial (documento 01)
        builder.HasIndex(s => s.TenantId)
            .IsUnique()
            .HasDatabaseName("uq_store_default")
            .HasFilter("is_default AND deleted_at IS NULL");

        // Device, EdgeInstallation e Station são configurados a partir do lado filho.
    }
}
