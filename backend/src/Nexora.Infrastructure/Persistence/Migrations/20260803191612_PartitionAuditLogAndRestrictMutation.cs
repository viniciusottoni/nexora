using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// E-09/US-090 — fecha os dois gaps de imutabilidade e volume deixados pela migration
    /// <c>InitialCreate</c>: (1) <c>audit_log</c> era uma tabela única sem partição, apesar de o
    /// ADR-035 já decidir "particionamento mensal por <c>occurred_at</c>, 5 anos de retenção" para
    /// ela; (2) <c>app_user_role</c> tinha <c>UPDATE</c> concedido em <c>audit_log</c> (herdado do
    /// <c>GRANT ... UPDATE ON ALL TABLES</c> de <c>EnableRowLevelSecurity</c>) — a trilha não era
    /// imutável de fato, só por convenção de código.
    /// </summary>
    /// <remarks>
    /// Não há API tipada do EF Core para "converter tabela existente em particionada" (o particionamento
    /// declarativo do Postgres exige recriar a relação com <c>PARTITION BY RANGE</c> e mover a chave de
    /// partição para dentro da chave primária, ADR-035) — por isso esta migration inteira é SQL cru,
    /// seguindo a mesma técnica de <c>EnableRowLevelSecurity</c>. Sequência obrigatória (nomes de
    /// índice/política são únicos por schema, não por tabela — por isso os índices só são recriados
    /// DEPOIS de a tabela antiga já ter sido descartada):
    ///   1. renomeia audit_log -> audit_log_pre_partition (preserva os dados e os índices antigos sob o novo nome)
    ///   2. cria audit_log nova, particionada por RANGE(occurred_at), com PRIMARY KEY (id, occurred_at)
    ///   3. cria partições mensais cobrindo uma janela generosa (6 meses antes / 18 meses depois do
    ///      deploy) + uma partição DEFAULT para qualquer linha fora da janela — o job mensal que o
    ///      ADR-035 prevê para manter 2 meses futuros sempre prontos AINDA NÃO EXISTE neste código
    ///      (gap operacional documentado, fora do escopo de E-09: nenhum BackgroundService de
    ///      manutenção de partição existe hoje nem para domain_event, que também não está particionado)
    ///   4. copia os dados da tabela antiga para a nova (a partição DEFAULT absorve linha fora da janela)
    ///   5. descarta a tabela antiga (libera os nomes de índice/constraint/política para reuso)
    ///   6. recria os 4 índices (mesmos nomes de <c>AuditLogConfiguration</c>) sobre a tabela nova —
    ///      Postgres aplica o índice a cada partição já existente e a qualquer partição futura anexada
    ///   7. reaplica RLS (ENABLE/FORCE + POLICY tenant_isolation) — não sobrevive à recriação da tabela
    ///   8. concede SELECT/INSERT (nunca UPDATE) a app_user_role, ALL a platform_admin, SELECT a
    ///      app_readonly — a tabela nova não herda os GRANTs pontuais da migration original — e
    ///      REVOKE UPDATE, DELETE explícito (defensivo; nenhum dos dois foi concedido nesta tabela nova)
    /// </remarks>
    public partial class PartitionAuditLogAndRestrictMutation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE audit_log RENAME TO audit_log_pre_partition;");

            migrationBuilder.Sql(
                """
                CREATE TABLE audit_log (
                  id              uuid NOT NULL,
                  tenant_id       uuid NOT NULL,
                  store_id        uuid NULL,
                  actor_id        uuid NULL,
                  authorized_by   uuid NULL,
                  device_id       uuid NULL,
                  action          text NOT NULL,
                  entity          text NOT NULL,
                  entity_id       uuid NULL,
                  before          jsonb NULL,
                  after           jsonb NULL,
                  reason          text NULL,
                  ip              inet NULL,
                  domain_event_id uuid NULL,
                  trace_id        character varying(32) NULL,
                  occurred_at     timestamptz NOT NULL,
                  recorded_at     timestamptz NOT NULL DEFAULT now(),
                  PRIMARY KEY (id, occurred_at)
                ) PARTITION BY RANGE (occurred_at);
                """);

            // Janela estática (sem job de manutenção ainda, ver docstring da classe) — 6 meses
            // antes e 18 depois do momento em que a migration roda, mais uma partição DEFAULT para
            // qualquer linha fora da janela (evita erro de "no partition found" em vez de travar a
            // migration por causa de dado de teste/seed com data fora do intervalo).
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                  start_month date := date_trunc('month', (now() - interval '6 months'))::date;
                  end_month   date := date_trunc('month', (now() + interval '18 months'))::date;
                  cur date := start_month;
                  part_name text;
                BEGIN
                  WHILE cur < end_month LOOP
                    part_name := 'audit_log_' || to_char(cur, 'YYYY_MM');
                    EXECUTE format(
                      'CREATE TABLE IF NOT EXISTS %I PARTITION OF audit_log FOR VALUES FROM (%L) TO (%L)',
                      part_name, cur, cur + interval '1 month');
                    cur := cur + interval '1 month';
                  END LOOP;

                  EXECUTE 'CREATE TABLE IF NOT EXISTS audit_log_default PARTITION OF audit_log DEFAULT';
                END $$;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO audit_log (
                  id, tenant_id, store_id, actor_id, authorized_by, device_id, action, entity,
                  entity_id, before, after, reason, ip, domain_event_id, trace_id, occurred_at, recorded_at)
                SELECT
                  id, tenant_id, store_id, actor_id, authorized_by, device_id, action, entity,
                  entity_id, before, after, reason, ip, domain_event_id, trace_id, occurred_at, recorded_at
                FROM audit_log_pre_partition;
                """);

            migrationBuilder.Sql("DROP TABLE audit_log_pre_partition;");

            migrationBuilder.Sql("CREATE INDEX idx_audit_tenant_time ON audit_log (tenant_id, occurred_at DESC);");
            migrationBuilder.Sql("CREATE INDEX idx_audit_entity ON audit_log (tenant_id, entity, entity_id);");
            migrationBuilder.Sql("CREATE INDEX idx_audit_actor ON audit_log (tenant_id, actor_id, occurred_at DESC);");
            migrationBuilder.Sql("CREATE INDEX idx_audit_action ON audit_log (tenant_id, action, occurred_at DESC);");

            // RLS (ADR-004) não sobrevive à recriação da tabela — reaplicada aqui, mesma política
            // tenant_isolation da migration EnableRowLevelSecurity. Definida no PAI particionado;
            // Postgres aplica a política à hierarquia inteira quando a consulta passa pelo nome pai
            // (o único nome que o EF Core/Npgsql conhece — nenhuma consulta acessa partição por nome).
            migrationBuilder.Sql("ALTER TABLE audit_log ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE audit_log FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(
                "CREATE POLICY tenant_isolation ON audit_log USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());");

            // Grants — tabela nova não herda os GRANTs pontuais de EnableRowLevelSecurity (aqueles
            // só afetaram as tabelas que já existiam naquele momento). UPDATE nunca é concedido:
            // é isso que torna a trilha imutável de fato (US-090 §7 "REVOKE UPDATE, DELETE ON
            // audit_log FROM app_role").
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA public TO app_user_role, platform_admin, app_readonly;");
            migrationBuilder.Sql("GRANT SELECT, INSERT ON audit_log TO app_user_role;");
            migrationBuilder.Sql("GRANT ALL ON audit_log TO platform_admin;");
            migrationBuilder.Sql("GRANT SELECT ON audit_log TO app_readonly;");
            migrationBuilder.Sql("REVOKE UPDATE, DELETE ON audit_log FROM app_user_role;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE audit_log RENAME TO audit_log_partitioned;");

            migrationBuilder.Sql(
                """
                CREATE TABLE audit_log (
                  id              uuid NOT NULL,
                  tenant_id       uuid NOT NULL,
                  store_id        uuid NULL,
                  actor_id        uuid NULL,
                  authorized_by   uuid NULL,
                  device_id       uuid NULL,
                  action          text NOT NULL,
                  entity          text NOT NULL,
                  entity_id       uuid NULL,
                  before          jsonb NULL,
                  after           jsonb NULL,
                  reason          text NULL,
                  ip              inet NULL,
                  domain_event_id uuid NULL,
                  trace_id        character varying(32) NULL,
                  occurred_at     timestamptz NOT NULL,
                  recorded_at     timestamptz NOT NULL DEFAULT now(),
                  PRIMARY KEY (id)
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO audit_log (
                  id, tenant_id, store_id, actor_id, authorized_by, device_id, action, entity,
                  entity_id, before, after, reason, ip, domain_event_id, trace_id, occurred_at, recorded_at)
                SELECT
                  id, tenant_id, store_id, actor_id, authorized_by, device_id, action, entity,
                  entity_id, before, after, reason, ip, domain_event_id, trace_id, occurred_at, recorded_at
                FROM audit_log_partitioned;
                """);

            migrationBuilder.Sql("DROP TABLE audit_log_partitioned;");

            migrationBuilder.Sql("CREATE INDEX idx_audit_tenant_time ON audit_log (tenant_id, occurred_at DESC);");
            migrationBuilder.Sql("CREATE INDEX idx_audit_entity ON audit_log (tenant_id, entity, entity_id);");
            migrationBuilder.Sql("CREATE INDEX idx_audit_actor ON audit_log (tenant_id, actor_id, occurred_at DESC);");
            migrationBuilder.Sql("CREATE INDEX idx_audit_action ON audit_log (tenant_id, action, occurred_at DESC);");

            migrationBuilder.Sql("ALTER TABLE audit_log ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE audit_log FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(
                "CREATE POLICY tenant_isolation ON audit_log USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());");

            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE ON audit_log TO app_user_role;");
            migrationBuilder.Sql("GRANT ALL ON audit_log TO platform_admin;");
            migrationBuilder.Sql("GRANT SELECT ON audit_log TO app_readonly;");
            migrationBuilder.Sql("REVOKE DELETE ON audit_log FROM app_user_role;");
        }
    }
}
