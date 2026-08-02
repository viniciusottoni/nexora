using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

internal sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("area");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.StoreId).HasColumnName("store_id");
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasMany(a => a.Tables).WithOne(t => t.Area).HasForeignKey(t => t.AreaId);
    }
}
