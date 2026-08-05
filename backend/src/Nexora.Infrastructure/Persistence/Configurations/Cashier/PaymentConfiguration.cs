using Nexora.Domain.Cashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Cashier;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.StoreId).HasColumnName("store_id");
        builder.Property(p => p.SessionId).HasColumnName("session_id");
        builder.Property(p => p.OrderId).HasColumnName("order_id");
        builder.Property(p => p.CashSessionId).HasColumnName("cash_session_id");
        builder.Property(p => p.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(p => p.Method).HasColumnName("method");
        builder.Property(p => p.Status).HasColumnName("status").HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("money_amount");
        builder.Property(p => p.FeeAmount).HasColumnName("fee_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(p => p.NetAmount).HasColumnName("net_amount").HasColumnType("money_amount");
        builder.Property(p => p.TipAmount).HasColumnName("tip_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(p => p.ChangeAmount).HasColumnName("change_amount").HasColumnType("money_amount").HasDefaultValue(0m);

        builder.Property(p => p.Provider).HasColumnName("provider").HasMaxLength(32);
        builder.Property(p => p.ProviderRef).HasColumnName("provider_ref");
        builder.Property(p => p.ProviderPayload).HasColumnName("provider_payload").HasColumnType("jsonb"); // TODO: tipar quando o formato for definido
        builder.Property(p => p.Installments).HasColumnName("installments").HasColumnType("smallint").HasDefaultValue(1);
        builder.Property(p => p.CardBrand).HasColumnName("card_brand").HasMaxLength(20);
        builder.Property(p => p.AuthorizationCode).HasColumnName("authorization_code").HasMaxLength(32);

        builder.Property(p => p.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");
        builder.Property(p => p.RefundedAt).HasColumnName("refunded_at").HasColumnType("timestamptz");
        builder.Property(p => p.RefundAmount).HasColumnName("refund_amount").HasColumnType("money_amount");
        builder.Property(p => p.RefundReason).HasColumnName("refund_reason");
        builder.Property(p => p.AuthorizedBy).HasColumnName("authorized_by");

        // US-058 — conciliação contra o extrato do provedor (Fase 3 usa; aqui só a estrutura).
        builder.Property(p => p.ReconciliationStatus).HasColumnName("reconciliation_status").HasDefaultValue(PaymentReconciliationStatus.NotApplicable);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");

        builder.HasMany(p => p.Allocations).WithOne().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.BusinessDay, p.Method });
    }
}
