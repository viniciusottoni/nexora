using Awaken.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class RevenueCatEventConfiguration : IEntityTypeConfiguration<RevenueCatEvent>
{
    public void Configure(EntityTypeBuilder<RevenueCatEvent> builder)
    {
        builder.ToTable("revenue_cat_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AppUserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.OriginalTransactionId)
            .HasMaxLength(256);

        builder.Property(e => e.ProductId)
            .HasMaxLength(128);

        builder.Property(e => e.PayloadHash)
            .HasMaxLength(64);

        // Unique per event — primary idempotency guard.
        builder.HasIndex(e => e.EventId).IsUnique();

        // Query by subscriber (used for diagnostics / admin).
        builder.HasIndex(e => e.AppUserId);
    }
}
