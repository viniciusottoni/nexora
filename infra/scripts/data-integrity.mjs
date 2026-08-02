import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

export const DATA_INTEGRITY_SQL = `
WITH tenant_tables AS (
  SELECT c.oid, c.relname, c.relrowsecurity, c.relforcerowsecurity
  FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
  WHERE n.nspname = 'public' AND c.relkind = 'r'
    AND EXISTS (
      SELECT 1 FROM information_schema.columns i
      WHERE i.table_schema = 'public' AND i.table_name = c.relname AND i.column_name = 'tenant_id'
    )
), checks AS (
  SELECT 'rls_completo'::text AS check_name, count(*)::bigint AS failures
  FROM tenant_tables t
  WHERE NOT t.relrowsecurity OR NOT t.relforcerowsecurity OR NOT EXISTS (
    SELECT 1 FROM pg_policies p
    WHERE p.schemaname = 'public' AND p.tablename = t.relname
      AND p.policyname = 'tenant_isolation'
      AND p.qual IS NOT NULL AND p.with_check IS NOT NULL
  )
  UNION ALL
  SELECT 'eventos_duplicados', count(*) FROM (
    SELECT id FROM domain_event GROUP BY id HAVING count(*) > 1
  ) duplicate_event
  UNION ALL
  SELECT 'append_only', count(*) FROM (
    VALUES ('audit_log', 'trg_audit_immutable'), ('domain_event', 'trg_event_immutable')
  ) expected(table_name, trigger_name)
  WHERE NOT EXISTS (
    SELECT 1 FROM pg_trigger trigger
    JOIN pg_class relation ON relation.oid = trigger.tgrelid
    WHERE relation.relname = expected.table_name
      AND trigger.tgname = expected.trigger_name
      AND NOT trigger.tgisinternal
  )
)
SELECT check_name, failures FROM checks ORDER BY check_name;
`;

export async function runDataIntegrityCheck(client) {
  const result = await client.query(DATA_INTEGRITY_SQL);
  const checks = result.rows.map((row) => ({
    checkName: String(row.check_name),
    failures: Number(row.failures),
  }));
  const failed = checks.filter((check) => check.failures !== 0);
  if (failed.length > 0) {
    throw new Error(
      `Integridade violada: ${failed.map((check) => `${check.checkName}=${check.failures}`).join(', ')}.`,
    );
  }
  return checks;
}

function postgresClient(connectionString) {
  if (!connectionString) throw new Error('DATABASE_URL é obrigatório para integridade de dados.');
  const requireFromDb = createRequire(resolve('packages/db/package.json'));
  const { Client } = requireFromDb('pg');
  return new Client({ connectionString });
}

async function main() {
  const client = postgresClient(process.env.DATABASE_URL);
  await client.connect();
  try {
    const checks = await runDataIntegrityCheck(client);
    console.log(`Integridade de dados OK: ${checks.map((check) => check.checkName).join(', ')}.`);
  } finally {
    await client.end();
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch((error) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}
