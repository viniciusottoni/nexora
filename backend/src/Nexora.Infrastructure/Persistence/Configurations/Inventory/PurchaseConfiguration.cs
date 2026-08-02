using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchase");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.StoreId).HasColumnName("store_id");
        builder.Property(p => p.SupplierId).HasColumnName("supplier_id");
        builder.Property(p => p.Document).HasColumnName("document").HasMaxLength(60);
        builder.Property(p => p.Total).HasColumnName("total").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(p => p.PurchasedAt).HasColumnName("purchased_at").HasColumnType("timestamptz");
        builder.Property(p => p.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(p => p.Notes).HasColumnName("notes");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");

        builder.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PurchaseId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.SupplierId, p.Document })
            .IsUnique()
            .HasDatabaseName("uq_purchase_tenant_supplier_document");
    }
}
