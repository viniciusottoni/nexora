using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Ajusta <c>idempotency_key</c> (ADR-020) para funcionar em rotas sem tenant resolvido no
    /// momento da escrita — pareamento de dispositivo, provisionamento de tenant, registro/consumo
    /// de token de instalação. Ver comentário completo em
    /// <c>Nexora.Domain.Platform.IdempotencyKey</c> (desvio deliberado do SQL literal do ADR-020,
    /// que declara <c>tenant_id UUID NOT NULL</c>) e em
    /// <c>Nexora.Infrastructure.Idempotency.IdempotencyStore</c>.
    /// </summary>
    /// <inheritdoc />
    public partial class AdjustIdempotencyKeyTenantScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "idempotency_key",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // idempotency_key sai da política tenant_isolation aplicada em massa por
            // EnableRowLevelSecurity: com tenant_id nulo (rotas de plataforma/pareamento/
            // instalação), "tenant_id = current_tenant_id()" nunca é verdadeiro (NULL = qualquer
            // coisa), então o WITH CHECK recusaria a escrita mesmo com um tenant_id real gravado
            // por uma requisição autenticada comum — RLS fail-closed é o comportamento CERTO para
            // dado de negócio (ADR-004), mas ERRADO para esta tabela de plumbing cross-tenant.
            // Tratada como raiz global, mesma categoria de "tenant"/"unit_of_measure" (sem RLS).
            migrationBuilder.Sql(
                """
                ALTER TABLE idempotency_key NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE idempotency_key DISABLE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS tenant_isolation ON idempotency_key;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE POLICY tenant_isolation ON idempotency_key
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
                ALTER TABLE idempotency_key ENABLE ROW LEVEL SECURITY;
                ALTER TABLE idempotency_key FORCE ROW LEVEL SECURITY;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "idempotency_key",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
