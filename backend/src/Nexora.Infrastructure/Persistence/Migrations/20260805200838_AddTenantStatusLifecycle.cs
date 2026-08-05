using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status_version",
                table: "tenant",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "tenant_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    domain_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_status_history_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tenant_status_history_tenant",
                table: "tenant_status_history",
                columns: new[] { "tenant_id", "created_at" });

            // RLS (ADR-004, Docs/Domain/10) — mesmo padrão de AddPlatformScaleEpic para tabela nova
            // com tenant_id (governance.ts/Nexora.IntegrationTests verificam esta migration
            // dinamicamente, não é preciso atualizar nenhuma lista estática à parte).
            migrationBuilder.Sql(
                """
                ALTER TABLE tenant_status_history ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_status_history FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON tenant_status_history
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);

            // US-153 §3.1 "Migração/mapeamento explícito dos estados legados" — o enum interno
            // TRIAL (valor 0) foi renomeado para PROVISIONED (mesmo valor, ver Enums.cs); tenants
            // legados que já têm uma instalação edge concluída (o sinal técnico de "instalação
            // registrada", mesmo gatilho agora usado por RegisterInstallationCommandHandler) avançam
            // para INSTALLING (valor 4) — quem nunca teve instalação concluída permanece PROVISIONED.
            migrationBuilder.Sql(
                """
                UPDATE tenant
                SET status = 4, status_version = status_version + 1, updated_at = now()
                WHERE status = 0
                  AND EXISTS (
                    SELECT 1 FROM edge_installation
                    WHERE edge_installation.tenant_id = tenant.id
                      AND edge_installation.installed_at IS NOT NULL
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON tenant_status_history;");

            migrationBuilder.DropTable(
                name: "tenant_status_history");

            migrationBuilder.DropColumn(
                name: "status_version",
                table: "tenant");
        }
    }
}
