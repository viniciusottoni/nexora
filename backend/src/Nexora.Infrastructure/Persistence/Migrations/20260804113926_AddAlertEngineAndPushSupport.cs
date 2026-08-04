using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertEngineAndPushSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "group_window_start",
                table: "alert",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pushed_at",
                table: "alert",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "push_subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    p256dh_key = table.Column<string>(type: "text", nullable: false),
                    auth_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_push_subscription", x => x.id);
                });

            // RLS (ADR-004, Docs/Domain/10) — mesma política tenant_isolation de toda tabela de
            // negócio com tenant_id (ver migration EnableRowLevelSecurity), aplicada aqui só à
            // tabela nova: os grants de app_user_role já cobrem push_subscription automaticamente
            // via ALTER DEFAULT PRIVILEGES (também definido em EnableRowLevelSecurity), então não é
            // preciso repetir GRANT aqui.
            migrationBuilder.Sql(
                """
                ALTER TABLE push_subscription ENABLE ROW LEVEL SECURITY;
                ALTER TABLE push_subscription FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON push_subscription
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);

            migrationBuilder.CreateIndex(
                name: "idx_alert_group",
                table: "alert",
                columns: new[] { "tenant_id", "group_key" },
                filter: "resolved_at IS NULL AND group_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_alert_open",
                table: "alert",
                columns: new[] { "tenant_id", "store_id", "severity", "raised_at" },
                filter: "acknowledged_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_push_subscription_tenant_id_endpoint",
                table: "push_subscription",
                columns: new[] { "tenant_id", "endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_push_subscription_tenant_id_user_id",
                table: "push_subscription",
                columns: new[] { "tenant_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON push_subscription;");

            migrationBuilder.DropTable(
                name: "push_subscription");

            migrationBuilder.DropIndex(
                name: "idx_alert_group",
                table: "alert");

            migrationBuilder.DropIndex(
                name: "idx_alert_open",
                table: "alert");

            migrationBuilder.DropColumn(
                name: "group_window_start",
                table: "alert");

            migrationBuilder.DropColumn(
                name: "pushed_at",
                table: "alert");
        }
    }
}
