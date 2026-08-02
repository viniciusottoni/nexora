using Awaken.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.UserId).IsRequired();

        builder.Property(i => i.ItemKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.Quantity).IsRequired();

        // RN-006: concorrencia otimista via coluna de sistema "xmin" do
        // PostgreSQL (MVCC), mesmo padrao de GoldWalletConfiguration -
        // mapeia coluna de sistema existente, nao requer migration.
        builder.Property(i => i.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(i => new { i.UserId, i.ItemKey }).IsUnique();
    }
}
