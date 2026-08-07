using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeAttentionAcknowledgement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administrative_attention_acknowledgement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<string>(type: "text", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_administrative_attention_acknowledgement", x => x.id);
                    table.ForeignKey(
                        name: "fk_administrative_attention_acknowledgement_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_administrative_attention_ack_tenant_item",
                table: "administrative_attention_acknowledgement",
                columns: new[] { "tenant_id", "item_id", "acknowledged_at" });

            // RLS (ADR-004) — administrative_attention_acknowledgement é tabela de negócio nova com
            // tenant_id (mesma política tenant_isolation de toda tabela nova, ver migration
            // AddOwnershipManagement/AddPlatformScaleEpic).
            migrationBuilder.Sql(
                """
                ALTER TABLE administrative_attention_acknowledgement ENABLE ROW LEVEL SECURITY;
                ALTER TABLE administrative_attention_acknowledgement FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON administrative_attention_acknowledgement
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                REVOKE UPDATE, DELETE ON administrative_attention_acknowledgement FROM app_user_role;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON administrative_attention_acknowledgement;");

            migrationBuilder.DropTable(
                name: "administrative_attention_acknowledgement");

        }
    }
}
