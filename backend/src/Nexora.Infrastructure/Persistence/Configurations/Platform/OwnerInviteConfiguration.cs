using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class OwnerInviteConfiguration : IEntityTypeConfiguration<OwnerInvite>
{
    public void Configure(EntityTypeBuilder<OwnerInvite> builder)
    {
        builder.ToTable("owner_invite");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.UserId).HasColumnName("user_id");
        builder.Property(i => i.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
        builder.Property(i => i.SecretHash).HasColumnName("secret_hash").IsRequired();
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(i => i.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamptz");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(i => new { i.TenantId, i.ExpiresAt }).HasDatabaseName("idx_owner_invite_tenant_expires");

        builder.HasOne(i => i.User).WithMany(u => u.Invites)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
