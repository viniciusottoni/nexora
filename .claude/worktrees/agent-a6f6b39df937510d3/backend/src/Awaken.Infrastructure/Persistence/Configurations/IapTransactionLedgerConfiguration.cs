using Awaken.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class IapTransactionLedgerConfiguration : IEntityTypeConfiguration<IapTransactionLedger>
{
    public void Configure(EntityTypeBuilder<IapTransactionLedger> builder)
    {
        builder.ToTable("iap_transaction_ledger");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TransactionId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProductKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Store).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.GrantedAtUtc);
        // Unique index on TransactionId ensures idempotency (ADR-010 pattern)
        builder.HasIndex(x => x.TransactionId).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
