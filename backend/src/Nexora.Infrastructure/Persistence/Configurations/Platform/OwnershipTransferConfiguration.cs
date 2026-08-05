using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>US-155 · Proprietários, usuários iniciais e convites — histórico imutável de transferência de titularidade.</summary>
internal sealed class OwnershipTransferConfiguration : IEntityTypeConfiguration<OwnershipTransfer>
{
    public void Configure(EntityTypeBuilder<OwnershipTransfer> builder)
    {
        builder.ToTable("ownership_transfer");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.TenantId).HasColumnName("tenant_id");
        builder.Property(t => t.PreviousOwnerUserId).HasColumnName("previous_owner_user_id");
        builder.Property(t => t.NewOwnerUserId).HasColumnName("new_owner_user_id");
        builder.Property(t => t.Reason).HasColumnName("reason").IsRequired();
        builder.Property(t => t.PreviousKeptAsAdmin).HasColumnName("previous_kept_as_admin");
        builder.Property(t => t.ActorId).HasColumnName("actor_id");
        builder.Property(t => t.TransferredAt).HasColumnName("transferred_at").HasColumnType("timestamptz");

        // Tabela de negócio comum com tenant_id — isolada por RLS (tenant_isolation), mesmo padrão
        // de tenant_status_history (US-153). Gravada sempre dentro de SetTenantContextAsync.
        builder.HasIndex(t => new { t.TenantId, t.TransferredAt }).HasDatabaseName("idx_ownership_transfer_tenant");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId);
    }
}
