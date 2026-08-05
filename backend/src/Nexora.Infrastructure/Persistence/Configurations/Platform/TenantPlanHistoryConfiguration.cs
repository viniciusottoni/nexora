using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>US-154 · Gestão de planos e configuração comercial — linha do tempo de mudanças de plano comercial (tabela de negócio com <c>tenant_id</c> e RLS normal, mesmo padrão de <c>TenantStatusHistoryConfiguration</c>).</summary>
internal sealed class TenantPlanHistoryConfiguration : IEntityTypeConfiguration<TenantPlanHistory>
{
    public void Configure(EntityTypeBuilder<TenantPlanHistory> builder)
    {
        builder.ToTable("tenant_plan_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(h => h.TenantId).HasColumnName("tenant_id");
        builder.Property(h => h.PreviousPlan).HasColumnName("previous_plan").HasMaxLength(32).IsRequired();
        builder.Property(h => h.NextPlan).HasColumnName("next_plan").HasMaxLength(32).IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason").IsRequired();
        builder.Property(h => h.ActorId).HasColumnName("actor_id");
        builder.Property(h => h.RequestedAt).HasColumnName("requested_at").HasColumnType("timestamptz");
        builder.Property(h => h.EffectiveAt).HasColumnName("effective_at").HasColumnType("timestamptz");
        builder.Property(h => h.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz");
        builder.Property(h => h.DomainEventId).HasColumnName("domain_event_id");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.Ignore(h => h.IsPending);

        // Mesma exceção documentada em TenantStatusHistoryConfiguration: filha de tenant (a única
        // tabela sem RLS), mas ela própria É de negócio comum — RLS normal via tenant_isolation,
        // sempre gravada dentro de SetTenantContextAsync.
        builder.HasIndex(h => new { h.TenantId, h.RequestedAt }).HasDatabaseName("idx_tenant_plan_history_tenant");

        // Suporta a busca por "linha pendente mais recente" de GetTenantPlanQueryHandler
        // (applied_at IS NULL) sem varrer o histórico inteiro por tenant.
        builder.HasIndex(h => new { h.TenantId, h.AppliedAt }).HasDatabaseName("idx_tenant_plan_history_pending");

        builder.HasOne(h => h.Tenant).WithMany().HasForeignKey(h => h.TenantId);
    }
}
