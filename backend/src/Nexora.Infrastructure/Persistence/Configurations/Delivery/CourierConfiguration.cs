using Nexora.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Delivery;

internal sealed class CourierConfiguration : IEntityTypeConfiguration<Courier>
{
    public void Configure(EntityTypeBuilder<Courier> builder)
    {
        builder.ToTable("courier");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.StoreId).HasColumnName("store_id");
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(c => c.Vehicle).HasColumnName("vehicle").HasMaxLength(20);
        builder.Property(c => c.Plate).HasColumnName("plate").HasMaxLength(10);
        builder.Property(c => c.IsOwn).HasColumnName("is_own").HasDefaultValue(true);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
    }
}
