using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryCountConfiguration : IEntityTypeConfiguration<InventoryCount>
{
    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.ToTable("inventory_count");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.StoreId).HasColumnName("store_id");
        builder.Property(c => c.BusinessDay).HasColumnName("business_day").HasColumnType("date");

        // Status é VARCHAR livre no schema de origem, não enum nativo do Postgres — sem
        // HasPostgresEnum/MapEnum aqui, diferente dos demais campos de status deste pacote.
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(16).HasDefaultValue("OPEN");

        builder.Property(c => c.CountedAt).HasColumnName("counted_at").HasColumnType("timestamptz");
        builder.Property(c => c.CountedBy).HasColumnName("counted_by");
        builder.Property(c => c.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz");
        builder.Property(c => c.TotalDivergenceCost).HasColumnName("total_divergence_cost").HasColumnType("money_amount");
        builder.Property(c => c.Notes).HasColumnName("notes");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CountId).OnDelete(DeleteBehavior.Cascade);
    }
}
