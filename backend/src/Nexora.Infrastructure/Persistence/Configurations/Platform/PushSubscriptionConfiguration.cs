using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscription");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.Endpoint).HasColumnName("endpoint").IsRequired();
        builder.Property(s => s.P256dhKey).HasColumnName("p256dh_key").IsRequired();
        builder.Property(s => s.AuthKey).HasColumnName("auth_key").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // Reassinar o mesmo endpoint (ex.: reload da página) atualiza a assinatura em vez de duplicar.
        builder.HasIndex(s => new { s.TenantId, s.Endpoint }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.UserId });
    }
}
