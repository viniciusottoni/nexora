using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.ToTable("price", table =>
        {
            table.HasCheckConstraint("ck_price_amount_non_negative", "amount >= 0");
            table.HasCheckConstraint("ck_price_valid_period", "valid_to IS NULL OR valid_to > valid_from");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.VariantId).HasColumnName("variant_id");
        builder.Property(p => p.Channel).HasColumnName("channel");

        // dinheiro — domínio money_amount, tipo C# decimal (ADR-017)
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("money_amount");

        builder.Property(p => p.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamptz");
        builder.Property(p => p.ValidTo).HasColumnName("valid_to").HasColumnType("timestamptz");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");

        // relação ProductVariant -> Prices configurada uma única vez em ProductVariantConfiguration (lado "um")

        builder.HasIndex(p => new { p.TenantId, p.VariantId, p.Channel, p.ValidFrom })
            .HasDatabaseName("idx_price_tenant_variant_channel_valid_from")
            .IsDescending(false, false, false, true);

        builder.HasIndex(p => new { p.TenantId, p.VariantId, p.Channel })
            .HasDatabaseName("uq_price_current_tenant_variant_channel")
            .IsUnique()
            .HasFilter("valid_to IS NULL");
    }
}
