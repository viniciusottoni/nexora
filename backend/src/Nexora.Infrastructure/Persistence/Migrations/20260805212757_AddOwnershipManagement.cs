using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnershipManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "email_outbox_id",
                table: "owner_invite",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "owner_invite",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "owner_invite",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "owner_invite",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ownership_transfer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    previous_kept_as_admin = table.Column<bool>(type: "boolean", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transferred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ownership_transfer", x => x.id);
                    table.ForeignKey(
                        name: "fk_ownership_transfer_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_owner_invite_tenant_user_created",
                table: "owner_invite",
                columns: new[] { "tenant_id", "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_ownership_transfer_tenant",
                table: "ownership_transfer",
                columns: new[] { "tenant_id", "transferred_at" });

            // RLS (ADR-004) — ownership_transfer é tabela de negócio nova com tenant_id (mesma
            // política tenant_isolation de toda tabela nova, ver migration AddPlatformScaleEpic).
            // owner_invite já tinha RLS habilitado desde EnableRowLevelSecurity; colunas novas
            // adicionadas acima não exigem reconfiguração de política.
            migrationBuilder.Sql(
                """
                ALTER TABLE ownership_transfer ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ownership_transfer FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON ownership_transfer
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON ownership_transfer;");

            migrationBuilder.DropTable(
                name: "ownership_transfer");

            migrationBuilder.DropIndex(
                name: "idx_owner_invite_tenant_user_created",
                table: "owner_invite");

            migrationBuilder.DropColumn(
                name: "email_outbox_id",
                table: "owner_invite");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "owner_invite");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "owner_invite");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "owner_invite");
        }
    }
}
