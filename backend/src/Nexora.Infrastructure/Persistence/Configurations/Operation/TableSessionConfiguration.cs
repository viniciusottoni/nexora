using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

internal sealed class TableSessionConfiguration : IEntityTypeConfiguration<TableSession>
{
    public void Configure(EntityTypeBuilder<TableSession> builder)
    {
        builder.ToTable("table_session");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.StoreId).HasColumnName("store_id");
        builder.Property(s => s.TableId).HasColumnName("table_id");

        // business_day materializado na escrita — nunca calculado em tempo de consulta (ADR-018)
        builder.Property(s => s.BusinessDay).HasColumnName("business_day").HasColumnType("date");

        builder.Property(s => s.Status).HasColumnName("status").HasDefaultValue(TableSessionStatus.Open);
        builder.Property(s => s.GuestCount).HasColumnName("guest_count").HasColumnType("smallint").HasDefaultValue((short)1);

        // US-021/US-022: falso só quando aberta por QR — contagem pendente de confirmação do garçom.
        builder.Property(s => s.GuestCountConfirmed).HasColumnName("guest_count_confirmed").HasDefaultValue(true);

        builder.Property(s => s.WaiterId).HasColumnName("waiter_id");
        builder.Property(s => s.OpenedBy).HasColumnName("opened_by");
        builder.Property(s => s.OpenedSource).HasColumnName("opened_source").HasMaxLength(16).HasDefaultValue("WAITER");
        builder.Property(s => s.OpenedAt).HasColumnName("opened_at").HasColumnType("timestamptz");
        builder.Property(s => s.BillRequestedAt).HasColumnName("bill_requested_at").HasColumnType("timestamptz");

        // US-026 §8: preferência de divisão registrada na solicitação da conta — ver docstring de
        // TableSession.SplitMode/SplitPeople.
        builder.Property(s => s.SplitMode).HasColumnName("split_mode").HasMaxLength(16);
        builder.Property(s => s.SplitPeople).HasColumnName("split_people").HasColumnType("smallint");
        builder.Property(s => s.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamptz");
        builder.Property(s => s.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamptz");

        // dinheiro — domínio money_amount, tipo C# decimal (ADR-017)
        builder.Property(s => s.Subtotal).HasColumnName("subtotal").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(s => s.DiscountAmount).HasColumnName("discount_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(s => s.ServiceFeeAmount).HasColumnName("service_fee_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(s => s.TotalAmount).HasColumnName("total_amount").HasColumnType("money_amount").HasDefaultValue(0m);

        // US-054 (Desconto com autorização) — escopo SESSION (RN-011).
        builder.Property(s => s.DiscountPercent).HasColumnName("discount_percent").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(s => s.DiscountReason).HasColumnName("discount_reason");
        builder.Property(s => s.DiscountAppliedBy).HasColumnName("discount_applied_by");
        builder.Property(s => s.DiscountAuthorizedBy).HasColumnName("discount_authorized_by");
        builder.Property(s => s.DiscountScope).HasColumnName("discount_scope").HasMaxLength(16);

        // US-053 (Taxa de serviço com retirada registrada) — registro autoritativo no nível da sessão.
        builder.Property(s => s.ServiceFeeWaived).HasColumnName("service_fee_waived").HasDefaultValue(false);
        builder.Property(s => s.ServiceFeeWaiveReason).HasColumnName("service_fee_waive_reason");
        builder.Property(s => s.ServiceFeeWaivedBy).HasColumnName("service_fee_waived_by");
        builder.Property(s => s.ServiceFeeWaiveScope).HasColumnName("service_fee_waive_scope").HasMaxLength(16);

        builder.Property(s => s.Rating).HasColumnName("rating").HasColumnType("smallint");
        builder.Property(s => s.RatingComment).HasColumnName("rating_comment");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(s => s.Table).WithMany().HasForeignKey(s => s.TableId);
        builder.HasMany(s => s.Orders).WithOne(o => o.Session).HasForeignKey(o => o.SessionId);

        builder.HasIndex(s => new { s.TenantId, s.BusinessDay }).HasDatabaseName("idx_table_session_tenant_business_day");
    }
}
