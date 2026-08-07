using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installation_credential",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installation_credential", x => x.id);
                    table.ForeignKey(
                        name: "fk_installation_credential_edge_installation_installation_id",
                        column: x => x.installation_id,
                        principalTable: "edge_installation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_installation_credential_installation",
                table: "installation_credential",
                columns: new[] { "tenant_id", "installation_id" });

            migrationBuilder.CreateIndex(
                name: "idx_installation_credential_pending",
                table: "installation_credential",
                columns: new[] { "installation_id", "revoked_at", "consumed_at" },
                filter: "revoked_at IS NULL AND consumed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_installation_credential_token_hash",
                table: "installation_credential",
                column: "token_hash",
                unique: true);

            // RLS (ADR-004, Docs/Domain/10) — mesma política tenant_isolation de toda tabela de
            // negócio com tenant_id (ver migration EnableRowLevelSecurity). Os grants de
            // app_user_role já cobrem esta tabela automaticamente via ALTER DEFAULT PRIVILEGES,
            // então não é preciso repetir GRANT aqui (mesma nota de AddPlatformScaleEpic).
            migrationBuilder.Sql(
                """
                ALTER TABLE installation_credential ENABLE ROW LEVEL SECURITY;
                ALTER TABLE installation_credential FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON installation_credential
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON installation_credential;");

            migrationBuilder.DropTable(
                name: "installation_credential");
        }
    }
}
