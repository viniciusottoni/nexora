using Awaken.Domain.Entities.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class GoldLedgerEntryConfiguration : IEntityTypeConfiguration<GoldLedgerEntry>
{
    public void Configure(EntityTypeBuilder<GoldLedgerEntry> builder)
    {
        builder.ToTable("gold_ledger_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WalletId).IsRequired();

        builder.Property(e => e.Direction)
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();

        builder.Property(e => e.Amount).IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.ReferenceType).HasMaxLength(64);
        builder.Property(e => e.ReferenceId).HasMaxLength(64);
        builder.Property(e => e.BalanceAfter).IsRequired();
        builder.Property(e => e.CorrelationId).HasMaxLength(64);

        // Append-only: nenhuma mutacao apos a criacao (RN-002).
        builder.HasIndex(e => new { e.WalletId, e.CreatedAtUtc });

        // US-228: reconciliacao da economia Gold cruza ledger entries por
        // ReferenceType+ReferenceId (ex.: "shop_order"+orderId) para checar
        // RN-002 (pedido granted sem debito) e RN-003 (credito sem validacao).
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });
    }
}
