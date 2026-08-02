import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

export const METRICS_RECALCULATION_SQL = `
WITH raw AS (
  SELECT count(*)::bigint AS event_count FROM domain_event
), hourly AS (
  SELECT date_trunc('hour', occurred_at) AS occurred_hour, count(*)::bigint AS event_count
  FROM domain_event
  GROUP BY date_trunc('hour', occurred_at)
)
SELECT raw.event_count AS raw_events,
       COALESCE((SELECT sum(hourly.event_count) FROM hourly), 0)::bigint AS bucketed_events
FROM raw;
`;

export async function runMetricsRecalculationCheck(client) {
  const result = await client.query(METRICS_RECALCULATION_SQL);
  const row = result.rows[0];
  if (!row) throw new Error('Recálculo comparativo não retornou resultado.');
  const rawEvents = Number(row.raw_events);
  const bucketedEvents = Number(row.bucketed_events);
  if (rawEvents !== bucketedEvents) {
    throw new Error(
      `Divergência no recálculo por occurred_at: bruto=${rawEvents}, buckets=${bucketedEvents}.`,
    );
  }
  return { rawEvents, bucketedEvents };
}

function postgresClient(connectionString) {
  if (!connectionString) throw new Error('DATABASE_URL é obrigatório para recálculo de métricas.');
  const requireFromDb = createRequire(resolve('packages/db/package.json'));
  const { Client } = requireFromDb('pg');
  return new Client({ connectionString });
}

async function main() {
  const client = postgresClient(process.env.DATABASE_URL);
  await client.connect();
  try {
    const result = await runMetricsRecalculationCheck(client);
    console.log(`Recálculo de métricas OK: ${result.rawEvents} eventos conciliados.`);
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
