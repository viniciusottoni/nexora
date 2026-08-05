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

        // US-155 (Proprietários, usuários iniciais e convites) — camada administrativa sobre o
        // convite já existente: revogação, motivo do reenvio/correção e correlação com o e-mail
        // enfileirado (ver docstring de cada propriedade em OwnerInvite.cs).
        builder.Property(i => i.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(i => i.RevokedReason).HasColumnName("revoked_reason");
        builder.Property(i => i.Reason).HasColumnName("reason");
        builder.Property(i => i.EmailOutboxId).HasColumnName("email_outbox_id");

        builder.HasIndex(i => new { i.TenantId, i.ExpiresAt }).HasDatabaseName("idx_owner_invite_tenant_expires");

        // US-155 — histórico administrativo lista convites por usuário (dono) ordenado por criação;
        // esta consulta (GetTenantOwnershipQueryHandler) roda a cada carregamento da tela.
        builder.HasIndex(i => new { i.TenantId, i.UserId, i.CreatedAt }).HasDatabaseName("idx_owner_invite_tenant_user_created");

        builder.HasOne(i => i.User).WithMany(u => u.Invites)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
