using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Habilita Row Level Security (ADR-004, Docs/Domain/10-RLS-Papeis-e-Indices.md) em toda
    /// tabela de negócio com <c>tenant_id</c> — o gap mais crítico identificado pela auditoria de
    /// US-001: sem esta migration, o isolamento multi-tenant dependia inteiramente de disciplina
    /// de código (nenhum <c>WHERE tenant_id</c> era estruturalmente obrigatório).
    /// </summary>
    /// <remarks>
    /// A lista de tabelas é a interseção real do schema gerado por <c>InitialCreate</c> com
    /// "tem coluna tenant_id" — mais abrangente que o array literal do documento 10 (que não
    /// inclui tabelas adicionadas depois por Auth/Devices/Installations: auth_attempt,
    /// auth_session, email_outbox, installation_nonce, owner_invite, pairing_code). Ficam de
    /// fora, mesmo tendo <c>tenant_id</c>, apenas <c>outbox</c> e <c>sync_cursor</c> — exceção
    /// documentada no próprio documento 10 §2 ("apenas no edge, single-tenant; sem RLS"). Tabelas
    /// verdadeiramente globais (<c>tenant</c>, <c>unit_of_measure</c>) não têm <c>tenant_id</c> e
    /// nunca entram nesta lista.
    /// </remarks>
    public partial class EnableRowLevelSecurity : Migration
    {
        // Toda tabela de negócio com tenant_id, exceto outbox/sync_cursor (edge, single-tenant,
        // exceção documentada no doc. 10 §2) — mesma lista usada por
        // infra/scripts/governance.ts e por Nexora.IntegrationTests para não divergir da migration.
        internal static readonly string[] TenantIsolatedTables =
        {
            "alert", "app_user", "area", "audit_log", "auth_attempt", "auth_session",
            "cash_movement", "cash_session", "category", "courier", "customer",
            "customer_address", "delivery_run", "delivery_stop", "delivery_zone", "device",
            "dining_table", "domain_event", "edge_installation", "email_outbox", "employee",
            "expense_category", "financial_account", "financial_entry", "goal",
            "idempotency_key", "ingredient", "installation_nonce", "inventory_count",
            "inventory_count_item", "media_asset", "metric_daily", "metric_hourly",
            "metric_operator_daily", "metric_product_daily", "modifier", "modifier_group",
            "order", "order_item", "order_item_fraction", "order_item_modifier",
            "owner_invite", "pairing_code", "payment", "payment_allocation", "payroll",
            "payroll_item", "price", "product", "product_modifier_group", "product_variant",
            "purchase", "purchase_item", "recipe", "recipe_item", "role", "station",
            "stock_movement", "store", "supplier", "sync_conflict", "table_session",
            "tenant_config", "tenant_secret", "user_role",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------------------------------
            // Papéis de banco (Docs/Domain/10 §1). NOLOGIN — quem efetivamente conecta é um
            // USER de deploy (CREATE USER ... IN ROLE ...), fora desta migration porque
            // carrega senha (ADR-031, segredo não pertence a versionamento). platform_admin é
            // a ÚNICA exceção com BYPASSRLS nesta solution, e só pode ser usada por rota
            // marcada como módulo de plataforma, sempre com registro em audit_log.
            // -----------------------------------------------------------------------------
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_user_role') THEN
                    CREATE ROLE app_user_role NOLOGIN;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'platform_admin') THEN
                    CREATE ROLE platform_admin NOLOGIN BYPASSRLS;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_readonly') THEN
                    CREATE ROLE app_readonly NOLOGIN;
                  END IF;
                END $$;
                """);

            // -----------------------------------------------------------------------------
            // RLS em massa (Docs/Domain/10 §2) — USING filtra leitura, WITH CHECK impede
            // gravar linha de outro tenant; os dois são obrigatórios (só o primeiro permitiria
            // INSERT/UPDATE com tenant_id divergente). current_tenant_id() (criada na migration
            // InitialCreate) retorna NULL sem contexto -> nenhuma linha satisfaz a comparação
            // -> falha fechada.
            // -----------------------------------------------------------------------------
            var tableArray = string.Join(",", TenantIsolatedTables.Select(t => $"'{t}'"));
            migrationBuilder.Sql(
                $"""
                DO $$
                DECLARE t text;
                BEGIN
                  FOREACH t IN ARRAY ARRAY[{tableArray}] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', t);
                    EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I', t);
                    EXECUTE format(
                      'CREATE POLICY tenant_isolation ON %I USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id())',
                      t);
                  END LOOP;
                END $$;
                """);

            // -----------------------------------------------------------------------------
            // Grants (Docs/Domain/10 §4) — app_user_role nunca recebe DELETE físico (soft
            // delete é a única forma de remoção, doc. Domain/00 §5); tenant_secret só é
            // acessível pelo papel de plataforma.
            // -----------------------------------------------------------------------------
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA public TO app_user_role, platform_admin, app_readonly;");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO app_user_role;");
            migrationBuilder.Sql("GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app_user_role;");
            migrationBuilder.Sql("GRANT ALL ON ALL TABLES IN SCHEMA public TO platform_admin;");
            migrationBuilder.Sql("GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_readonly;");
            migrationBuilder.Sql("REVOKE ALL ON tenant_secret FROM app_user_role, app_readonly;");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE ON tenant_secret TO platform_admin;");
            migrationBuilder.Sql("REVOKE DELETE ON ALL TABLES IN SCHEMA public FROM app_user_role;");
            migrationBuilder.Sql(
                "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE ON TABLES TO app_user_role;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tableArray = string.Join(",", TenantIsolatedTables.Select(t => $"'{t}'"));
            migrationBuilder.Sql(
                $"""
                DO $$
                DECLARE t text;
                BEGIN
                  FOREACH t IN ARRAY ARRAY[{tableArray}] LOOP
                    EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I', t);
                    EXECUTE format('ALTER TABLE %I NO FORCE ROW LEVEL SECURITY', t);
                    EXECUTE format('ALTER TABLE %I DISABLE ROW LEVEL SECURITY', t);
                  END LOOP;
                END $$;
                """);

            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE SELECT, INSERT, UPDATE ON TABLES FROM app_user_role;");
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_user_role') THEN
                    EXECUTE 'DROP OWNED BY app_user_role';
                    EXECUTE 'DROP ROLE app_user_role';
                  END IF;
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'platform_admin') THEN
                    EXECUTE 'DROP OWNED BY platform_admin';
                    EXECUTE 'DROP ROLE platform_admin';
                  END IF;
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_readonly') THEN
                    EXECUTE 'DROP OWNED BY app_readonly';
                    EXECUTE 'DROP ROLE app_readonly';
                  END IF;
                END $$;
                """);
        }
    }
}
