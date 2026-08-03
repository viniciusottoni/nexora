using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

internal sealed class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("dining_table");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.TenantId).HasColumnName("tenant_id");
        builder.Property(t => t.StoreId).HasColumnName("store_id");
        builder.Property(t => t.AreaId).HasColumnName("area_id");
        builder.Property(t => t.Label).HasColumnName("label").HasMaxLength(16).IsRequired();
        builder.Property(t => t.Seats).HasColumnName("seats").HasColumnType("smallint").HasDefaultValue((short)4);
        builder.Property(t => t.QrToken).HasColumnName("qr_token").IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasDefaultValue(TableStatus.Free);
        builder.Property(t => t.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // relação Area -> Tables configurada uma única vez em AreaConfiguration (lado "um")

        builder.HasIndex(t => t.QrToken).IsUnique().HasDatabaseName("uq_dining_table_qr_token");
        builder.HasIndex(t => new { t.StoreId, t.Label }).IsUnique().HasDatabaseName("uq_dining_table_store_label");

        // Docs/Domain/03-Operacao.md: "CREATE INDEX idx_table_store_status ON dining_table
        // (tenant_id, store_id, status) WHERE deleted_at IS NULL AND is_active" — índice parcial
        // que sustenta o mapa de salão/contagem de mesas ativas por status (US-020 §11: "mesas
        // ativas por ambiente"), consultado a cada abertura de mesa/troca de status.
        builder.HasIndex(t => new { t.TenantId, t.StoreId, t.Status })
            .HasDatabaseName("idx_table_store_status")
            .HasFilter("deleted_at IS NULL AND is_active");
    }
}
