using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-151 "Diretório de estabelecimentos com busca e filtros" — espelha
    /// <c>owner_email</c>/<c>template_code</c> na única tabela sem RLS (ver docstring de
    /// <c>Tenant.OwnerEmail</c>/<c>Tenant.TemplateCode</c>) e cria os índices que o diretório
    /// precisa (DoD §14 "índices e plano de execução verificados"): <c>idx_tenant_status</c>/
    /// <c>idx_tenant_created_at</c>/<c>idx_tenant_template_code</c> via Fluent API (colunas
    /// simples); <c>idx_tenant_name</c>/<c>idx_tenant_owner_email</c> são índices de EXPRESSÃO
    /// (<c>lower(...)</c>, para <c>ILIKE</c> case-insensitive) — sem suporte nativo na Fluent API do
    /// EF Core, por isso via SQL cru aqui.
    /// </summary>
    /// <inheritdoc />
    public partial class AddTenantDirectorySearchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner_email",
                table: "tenant",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_code",
                table: "tenant",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_tenant_created_at",
                table: "tenant",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_status",
                table: "tenant",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_template_code",
                table: "tenant",
                column: "template_code");

            migrationBuilder.Sql(
                "CREATE INDEX idx_tenant_name ON tenant (lower(name));");

            migrationBuilder.Sql(
                "CREATE INDEX idx_tenant_owner_email ON tenant (lower(owner_email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_tenant_owner_email;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_tenant_name;");

            migrationBuilder.DropIndex(
                name: "idx_tenant_created_at",
                table: "tenant");

            migrationBuilder.DropIndex(
                name: "idx_tenant_status",
                table: "tenant");

            migrationBuilder.DropIndex(
                name: "idx_tenant_template_code",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "owner_email",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "template_code",
                table: "tenant");
        }
    }
}
