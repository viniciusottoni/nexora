using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("product_modifier_group");

        // chave primária composta — sem coluna "id" própria (schema.prisma: @@id([productId, groupId]))
        builder.HasKey(pg => new { pg.ProductId, pg.GroupId });

        builder.Property(pg => pg.TenantId).HasColumnName("tenant_id");
        builder.Property(pg => pg.ProductId).HasColumnName("product_id");
        builder.Property(pg => pg.GroupId).HasColumnName("group_id");
        builder.Property(pg => pg.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);

        builder.HasOne(pg => pg.Product).WithMany(p => p.ProductModifierGroups).HasForeignKey(pg => pg.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pg => pg.Group).WithMany(g => g.ProductModifierGroups).HasForeignKey(pg => pg.GroupId).OnDelete(DeleteBehavior.Cascade);
    }
}
