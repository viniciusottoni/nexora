using Awaken.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class InventorySlotConfiguration : IEntityTypeConfiguration<InventorySlot>
{
    public void Configure(EntityTypeBuilder<InventorySlot> builder)
    {
        builder.ToTable("inventory_slots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();

        builder.Property(s => s.SlotKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.ItemKey)
            .HasMaxLength(64);

        builder.HasIndex(s => new { s.UserId, s.SlotKey }).IsUnique();
    }
}
