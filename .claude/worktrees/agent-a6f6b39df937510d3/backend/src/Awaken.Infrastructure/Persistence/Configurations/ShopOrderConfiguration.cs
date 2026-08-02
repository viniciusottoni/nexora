using Awaken.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ShopOrderConfiguration : IEntityTypeConfiguration<ShopOrder>
{
    public void Configure(EntityTypeBuilder<ShopOrder> builder)
    {
        builder.ToTable("shop_orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).IsRequired();

        builder.Property(o => o.Channel)
            .IsRequired()
            .HasMaxLength(16); // "gold" | "iap"

        builder.Property(o => o.ProductKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(16); // "pending" | "granted" | "failed" | "refunded"

        // Único por transação IAP (ADR-010 / RN-003).
        builder.Property(o => o.ExternalTransactionId).HasMaxLength(256);
        builder.HasIndex(o => o.ExternalTransactionId)
            .IsUnique()
            .HasFilter("\"ExternalTransactionId\" IS NOT NULL");

        builder.Property(o => o.CorrelationId).HasMaxLength(64);

        builder.Property(o => o.GrantedAtUtc);
        builder.Property(o => o.FailedAtUtc);
        builder.Property(o => o.RefundedAtUtc);

        // Consultas por usuário e por status.
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
    }
}
