using Nexora.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Delivery;

internal sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_address");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.CustomerId).HasColumnName("customer_id");
        builder.Property(a => a.ZoneId).HasColumnName("zone_id");
        builder.Property(a => a.Label).HasColumnName("label").HasMaxLength(32);
        builder.Property(a => a.Street).HasColumnName("street").IsRequired();
        builder.Property(a => a.Number).HasColumnName("number").HasMaxLength(16);
        builder.Property(a => a.Complement).HasColumnName("complement");
        builder.Property(a => a.District).HasColumnName("district");
        builder.Property(a => a.City).HasColumnName("city").IsRequired();
        builder.Property(a => a.State).HasColumnName("state").HasColumnType("char(2)");
        builder.Property(a => a.Zip).HasColumnName("zip").HasMaxLength(9);
        builder.Property(a => a.Reference).HasColumnName("reference");
        builder.Property(a => a.Lat).HasColumnName("lat").HasColumnType("numeric(10,7)");
        builder.Property(a => a.Lng).HasColumnName("lng").HasColumnType("numeric(10,7)");
        builder.Property(a => a.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.TenantId, a.CustomerId });
    }
}
