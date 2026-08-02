using Awaken.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ItemActiveEffectConfiguration : IEntityTypeConfiguration<ItemActiveEffect>
{
    public void Configure(EntityTypeBuilder<ItemActiveEffect> builder)
    {
        builder.ToTable("item_active_effects");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.ItemKey).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EffectType).IsRequired().HasMaxLength(32);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(16).HasDefaultValue("active");
        builder.Property(e => e.XpBoostMultiplier).HasColumnType("numeric(4,2)");
        builder.Property(e => e.StreakDaysToRestore);
        builder.Property(e => e.EffectDateUtc);
        builder.Property(e => e.ActivatedAtUtc).IsRequired();
        builder.Property(e => e.ExpiresAtUtc);
        builder.Property(e => e.ConsumedAtUtc);

        // Concorrência otimista (xmin) — mesmo padrão de InventoryItem/GoldWallet:
        // duas requisições concorrentes não devem conseguir consumir/contar o
        // mesmo efeito sem detectar conflito.
        builder.Property(e => e.Version).HasColumnName("xmin").IsRowVersion();

        builder.HasIndex(e => new { e.UserId, e.EffectType, e.Status })
            .HasDatabaseName("IX_item_active_effects_UserId_EffectType_Status");

        builder.HasIndex(e => new { e.UserId, e.EffectType, e.EffectDateUtc })
            .HasDatabaseName("IX_item_active_effects_UserId_EffectType_EffectDateUtc");
    }
}
