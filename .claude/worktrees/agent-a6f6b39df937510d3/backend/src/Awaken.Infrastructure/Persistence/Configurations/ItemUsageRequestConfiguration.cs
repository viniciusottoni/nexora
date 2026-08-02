using Awaken.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ItemUsageRequestConfiguration : IEntityTypeConfiguration<ItemUsageRequest>
{
    public void Configure(EntityTypeBuilder<ItemUsageRequest> builder)
    {
        builder.ToTable("item_usage_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.ItemKey).IsRequired().HasMaxLength(100);
        builder.Property(r => r.UseRequestId).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Success).IsRequired();
        builder.Property(r => r.EffectType).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Message).HasMaxLength(500);
        builder.Property(r => r.RemainingQuantity).IsRequired();
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        // US-230 RN-003: idempotência real — mesmo padrão global (sem escopo
        // por usuário) usado em ShopOrder.ExternalTransactionId.
        builder.HasIndex(r => r.UseRequestId)
            .IsUnique()
            .HasDatabaseName("IX_item_usage_requests_UseRequestId");

        builder.HasIndex(r => new { r.UserId, r.ItemKey, r.CreatedAtUtc })
            .HasDatabaseName("IX_item_usage_requests_UserId_ItemKey_CreatedAtUtc");
    }
}
