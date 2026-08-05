using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformScaleEpic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "template_code",
                table: "tenant_config",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "template_version",
                table: "tenant_config",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "activated_at",
                table: "tenant",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "onboarding_started_at",
                table: "tenant",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_update_at",
                table: "edge_installation",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_update_status",
                table: "edge_installation",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_version",
                table: "edge_installation",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "business_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    seeds = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "installation_incident",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cause = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installation_incident", x => x.id);
                    table.ForeignKey(
                        name: "fk_installation_incident_edge_installation_installation_id",
                        column: x => x.installation_id,
                        principalTable: "edge_installation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_step",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_step", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_step_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "release",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rollout_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "text", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_release", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_to = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access", x => x.id);
                    table.ForeignKey(
                        name: "fk_support_access_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_domain",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    verification_token = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cert_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cert_issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cert_expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_domain", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_domain_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // RLS (ADR-004, Docs/Domain/10) — mesma política tenant_isolation de toda tabela de
            // negócio com tenant_id (ver migration EnableRowLevelSecurity), aplicada aqui às quatro
            // tabelas novas com tenant_id: os grants de app_user_role já cobrem essas tabelas
            // automaticamente via ALTER DEFAULT PRIVILEGES, então não é preciso repetir GRANT aqui.
            // `business_template` e `release` NÃO entram aqui — são catálogo de plataforma sem
            // tenant_id (mesma natureza de `tenant`), a Replay é dona, não um estabelecimento.
            migrationBuilder.Sql(
                """
                ALTER TABLE installation_incident ENABLE ROW LEVEL SECURITY;
                ALTER TABLE installation_incident FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON installation_incident
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE onboarding_step ENABLE ROW LEVEL SECURITY;
                ALTER TABLE onboarding_step FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON onboarding_step
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE support_access ENABLE ROW LEVEL SECURITY;
                ALTER TABLE support_access FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON support_access
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE tenant_domain ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_domain FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON tenant_domain
                  USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);

            migrationBuilder.CreateIndex(
                name: "uq_business_template_code",
                table: "business_template",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_installation_incident_installation",
                table: "installation_incident",
                columns: new[] { "tenant_id", "installation_id" });

            migrationBuilder.CreateIndex(
                name: "idx_installation_incident_open",
                table: "installation_incident",
                columns: new[] { "installation_id", "resolved_at" },
                filter: "resolved_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_onboarding_step_tenant_key",
                table: "onboarding_step",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_release_version",
                table: "release",
                column: "version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_support_access_active",
                table: "support_access",
                columns: new[] { "tenant_id", "expires_at" },
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_support_access_tenant",
                table: "support_access",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_support_access_token_hash",
                table: "support_access",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tenant_domain_cert_expires",
                table: "tenant_domain",
                column: "cert_expires_at",
                filter: "cert_expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_domain_tenant",
                table: "tenant_domain",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_domain_domain",
                table: "tenant_domain",
                column: "domain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON installation_incident;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON onboarding_step;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON support_access;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON tenant_domain;");

            migrationBuilder.DropTable(
                name: "business_template");

            migrationBuilder.DropTable(
                name: "installation_incident");

            migrationBuilder.DropTable(
                name: "onboarding_step");

            migrationBuilder.DropTable(
                name: "release");

            migrationBuilder.DropTable(
                name: "support_access");

            migrationBuilder.DropTable(
                name: "tenant_domain");

            migrationBuilder.DropColumn(
                name: "template_code",
                table: "tenant_config");

            migrationBuilder.DropColumn(
                name: "template_version",
                table: "tenant_config");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "onboarding_started_at",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "last_update_at",
                table: "edge_installation");

            migrationBuilder.DropColumn(
                name: "last_update_status",
                table: "edge_installation");

            migrationBuilder.DropColumn(
                name: "target_version",
                table: "edge_installation");
        }
    }
}
