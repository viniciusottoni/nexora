using Awaken.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ItemUsageRecordConfiguration : IEntityTypeConfiguration<ItemUsageRecord>
{
    public void Configure(EntityTypeBuilder<ItemUsageRecord> builder)
    {
        builder.ToTable("item_usage_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();

        builder.Property(r => r.ItemKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.PeriodStartUtc).IsRequired();

        builder.Property(r => r.UsageCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.Property(r => r.UpdatedAtUtc);

        // US-230: índice único por (UserId, ItemKey, PeriodStartUtc) garante
        // que há no máximo um registro de uso por usuário/item/período.
        builder.HasIndex(r => new { r.UserId, r.ItemKey, r.PeriodStartUtc })
            .IsUnique()
            .HasDatabaseName("IX_item_usage_records_UserId_ItemKey_PeriodStartUtc");

        // Índice de suporte para consultas de uso por usuário/item.
        builder.HasIndex(r => new { r.UserId, r.ItemKey })
            .HasDatabaseName("IX_item_usage_records_UserId_ItemKey");
    }
}
