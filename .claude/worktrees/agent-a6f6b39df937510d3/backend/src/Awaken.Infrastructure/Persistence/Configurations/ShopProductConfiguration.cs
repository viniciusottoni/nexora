using Awaken.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ShopProductConfiguration : IEntityTypeConfiguration<ShopProduct>
{
    public void Configure(EntityTypeBuilder<ShopProduct> builder)
    {
        builder.ToTable("shop_products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Rarity).HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RevenueCatProductId).HasMaxLength(256);
        builder.Property(x => x.PriceGold);
        builder.Property(x => x.GoldAmount);
        builder.Property(x => x.Flavor).HasMaxLength(512);
        builder.Property(x => x.UsageLimit).HasMaxLength(32);
        builder.Ignore(x => x.Channel);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
