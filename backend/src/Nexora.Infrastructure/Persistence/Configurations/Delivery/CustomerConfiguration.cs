using Nexora.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Delivery;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customer");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Email).HasColumnName("email").HasColumnType("citext");
        builder.Property(c => c.Document).HasColumnName("document").HasMaxLength(18);
        builder.Property(c => c.Notes).HasColumnName("notes");
        builder.Property(c => c.AnonymizedAt).HasColumnName("anonymized_at").HasColumnType("timestamptz");
        builder.Property(c => c.LastOrderAt).HasColumnName("last_order_at").HasColumnType("timestamptz");
        builder.Property(c => c.OrdersCount).HasColumnName("orders_count").HasDefaultValue(0);
        builder.Property(c => c.TotalSpent).HasColumnName("total_spent").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
    }
}
