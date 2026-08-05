using Nexora.Domain.Cashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Cashier;

internal sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("cash_session", table =>
        {
            table.HasCheckConstraint("ck_cash_opening", "opening_amount >= 0");
            table.HasCheckConstraint(
                "ck_cash_closed",
                "status <> 2 OR (closed_at IS NOT NULL AND counted_amount IS NOT NULL)");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever(); // UUIDv7 na origem — ADR-016

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.StoreId).HasColumnName("store_id");
        builder.Property(c => c.OperatorId).HasColumnName("operator_id");
        builder.Property(c => c.DeviceId).HasColumnName("device_id");
        builder.Property(c => c.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(c => c.Status).HasColumnName("status").HasDefaultValue(CashSessionStatus.Open);

        builder.Property(c => c.OpeningAmount).HasColumnName("opening_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(c => c.ExpectedAmount).HasColumnName("expected_amount").HasColumnType("money_amount");
        builder.Property(c => c.CountedAmount).HasColumnName("counted_amount").HasColumnType("money_amount");
        builder.Property(c => c.Divergence).HasColumnName("divergence").HasColumnType("money_amount");

        builder.Property(c => c.OpenedAt).HasColumnName("opened_at").HasColumnType("timestamptz");
        builder.Property(c => c.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamptz");
        builder.Property(c => c.ClosedBy).HasColumnName("closed_by");
        builder.Property(c => c.AuthorizedBy).HasColumnName("authorized_by");
        builder.Property(c => c.Justification).HasColumnName("justification");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasMany(c => c.Movements).WithOne().HasForeignKey(m => m.CashSessionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.BusinessDay });

        // uq_cash_open (docs/domain/04-Caixa-e-Pagamento.md §"Regras de integridade" #1): um caixa
        // aberto por operador e loja — backstop de banco contra a corrida de duas aberturas
        // simultâneas (a checagem de aplicação vive em OpenCashSessionCommandHandler, US-055 §4,
        // cenário "Um caixa por operador e turno"). Filtro por inteiro (não por rótulo de enum
        // nativo): CashSessionStatus ainda é gravado como integer nesta solution — Closed = 2 (ver
        // mesma convenção documentada em OrderItemConfiguration, "status IN (0,1,2,3)").
        builder.HasIndex(c => new { c.StoreId, c.OperatorId })
            .IsUnique()
            .HasDatabaseName("uq_cash_open")
            .HasFilter("status <> 2");
    }
}
