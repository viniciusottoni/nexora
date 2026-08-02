using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variant");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(v => v.TenantId).HasColumnName("tenant_id");
        builder.Property(v => v.ProductId).HasColumnName("product_id");
        builder.Property(v => v.Name).HasColumnName("name").IsRequired();
        builder.Property(v => v.Sku).HasColumnName("sku").HasMaxLength(40);
        builder.Property(v => v.SizeCode).HasColumnName("size_code").HasMaxLength(16);
        builder.Property(v => v.PrepMinutes).HasColumnName("prep_minutes").HasColumnType("smallint").HasDefaultValue((short)10);
        builder.Property(v => v.WarnMinutes).HasColumnName("warn_minutes").HasColumnType("smallint");
        builder.Property(v => v.CriticalMinutes).HasColumnName("critical_minutes").HasColumnType("smallint");
        builder.Property(v => v.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(v => v.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(v => v.FiscalRates).HasColumnName("fiscal_rates").HasColumnType("jsonb");
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(v => v.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // relação Product -> Variants configurada uma única vez em ProductConfiguration (lado "um")
        builder.HasMany(v => v.Prices).WithOne(p => p.Variant).HasForeignKey(p => p.VariantId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.TenantId, v.ProductId }).HasDatabaseName("idx_product_variant_tenant_product");
    }
}
