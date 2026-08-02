using Awaken.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Plan)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(s => s.Entitlement)
            .HasMaxLength(128);

        builder.Property(s => s.RevenueCatCustomerId)
            .HasMaxLength(256);

        builder.HasIndex(s => s.UserId).IsUnique();
    }
}
