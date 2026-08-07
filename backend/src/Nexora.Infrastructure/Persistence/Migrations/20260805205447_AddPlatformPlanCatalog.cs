using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-154 "Gestão de planos e configuração comercial" — catálogo versionado de planos
    /// comerciais (<c>platform_plan</c>, sem <c>tenant_id</c>/RLS, mesmo padrão de
    /// <c>business_template</c>) e a linha do tempo de mudanças de plano por tenant
    /// (<c>tenant_plan_history</c>, COM RLS — mesma exceção documentada em
    /// <c>tenant_status_history</c>/<see cref="AddTenantStatusLifecycle"/>: filha de <c>tenant</c>
    /// mas de negócio comum). Semeia três planos ([HIPÓTESE] nomes/capacidades de exemplo — o
    /// modelo comercial real, preços e composição final dos planos são
    /// "[PENDÊNCIA BLOQUEANTE]" no doc da própria US, §15; ajustar quando o cliente/produto
    /// decidir) para que o catálogo nunca fique vazio e os tenants já existentes (todos com
    /// <c>plan = 'STANDARD'</c> por default de coluna) continuem resolvendo a um código válido.
    /// </summary>
    /// <inheritdoc />
    public partial class AddPlatformPlanCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "applied_plan_version",
                table: "tenant_config",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_capabilities",
                table: "tenant_config",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "plan_version",
                table: "tenant",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "platform_plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    capabilities = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    limits = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_plan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    next_plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    domain_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_history_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_platform_plan_code",
                table: "platform_plan",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tenant_plan_history_pending",
                table: "tenant_plan_history",
                columns: new[] { "tenant_id", "applied_at" });

            migrationBuilder.CreateIndex(
                name: "idx_tenant_plan_history_tenant",
                table: "tenant_plan_history",
                columns: new[] { "tenant_id", "requested_at" });

            // RLS (ADR-004, Docs/Domain/10) — tenant_plan_history é filha de tenant mas de negócio
            // COMUM (mesma exceção já documentada para tenant_status_history); platform_plan é
            // catálogo GLOBAL (mesmo padrão de business_template), sem tenant_id/RLS.
            migrationBuilder.Sql(
                """
                ALTER TABLE tenant_plan_history ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_plan_history FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON tenant_plan_history
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);

            var seedAt = new DateTimeOffset(2026, 8, 5, 20, 54, 47, TimeSpan.Zero);

            foreach (var (id, code, name, capabilitiesJson, limitsJson) in RowsToSeed())
            {
                migrationBuilder.InsertData(
                    table: "platform_plan",
                    columns: new[] { "id", "code", "name", "version", "capabilities", "limits", "is_active", "created_at", "updated_at" },
                    values: new object[] { id, code, name, 1, capabilitiesJson, limitsJson, true, seedAt, seedAt });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON tenant_plan_history;");

            foreach (var (id, _, _, _, _) in RowsToSeed())
            {
                migrationBuilder.DeleteData(table: "platform_plan", keyColumn: "id", keyValue: id);
            }

            migrationBuilder.DropTable(
                name: "platform_plan");

            migrationBuilder.DropTable(
                name: "tenant_plan_history");

            migrationBuilder.DropColumn(
                name: "applied_plan_version",
                table: "tenant_config");

            migrationBuilder.DropColumn(
                name: "plan_capabilities",
                table: "tenant_config");

            migrationBuilder.DropColumn(
                name: "plan_version",
                table: "tenant");
        }

        /// <summary>
        /// [HIPÓTESE] Catálogo de exemplo — GUIDs fixos (determinístico entre ambientes, mesmo
        /// padrão de <see cref="AddBusinessTemplateSeeds.RowsToSeed"/>), NÃO o modelo comercial
        /// final (US-154 §15, "[PENDÊNCIA BLOQUEANTE] modelo comercial, preços e composição final
        /// dos planos ainda precisam de decisão formal"). <c>STANDARD</c> é o default de coluna
        /// legado (<c>tenant.plan</c>) — precisa continuar existindo e ativo para que tenants já
        /// provisionados antes desta história continuem resolvendo a um código válido do catálogo.
        /// </summary>
        private static IEnumerable<(Guid Id, string Code, string Name, string CapabilitiesJson, string LimitsJson)> RowsToSeed()
        {
            yield return (
                new Guid("018f2b8a-0001-7000-8000-000000000001"),
                "STANDARD", "Standard",
                """["online_ordering","kds","cash_session"]""",
                """{"maxStores":1}""");
            yield return (
                new Guid("018f2b8a-0002-7000-8000-000000000002"),
                "GESTAO", "Gestão",
                """["online_ordering","kds","cash_session","inventory","delivery"]""",
                """{"maxStores":3}""");
            yield return (
                new Guid("018f2b8a-0003-7000-8000-000000000003"),
                "COMPLETO", "Completo",
                """["online_ordering","kds","cash_session","inventory","delivery","multi_store","advanced_reports"]""",
                """{"maxStores":null}""");
        }
    }
}
