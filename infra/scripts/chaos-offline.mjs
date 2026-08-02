import { pathToFileURL } from 'node:url';

function healthUrl(edgeUrl) {
  return new URL('/v1/health', edgeUrl.endsWith('/') ? edgeUrl : `${edgeUrl}/`);
}

export async function runChaosOfflineCheck({ edgeUrl, fetchImpl = fetch, timeoutMs = 5_000 }) {
  if (!edgeUrl) throw new Error('CHAOS_EDGE_URL é obrigatório para o ensaio offline.');

  let response;
  try {
    response = await fetchImpl(healthUrl(edgeUrl), { signal: AbortSignal.timeout(timeoutMs) });
  } catch (error) {
    throw new Error(`Edge não respondeu durante o corte de internet: ${String(error)}`);
  }
  if (!response.ok) throw new Error(`Edge respondeu HTTP ${response.status} durante o corte.`);

  const health = await response.json();
  if (health.postgres !== 'OK') {
    throw new Error(`PostgreSQL local precisa permanecer OK; recebido ${String(health.postgres)}.`);
  }
  if (health.sync === 'OK') {
    throw new Error('O sync continua OK: o ambiente de caos não está realmente offline.');
  }
  if (!['DOWN', 'DEGRADED', 'UNKNOWN'].includes(health.sync)) {
    throw new Error(`Estado de sync inválido durante offline: ${String(health.sync)}.`);
  }
  if (!Number.isInteger(health.pendingEvents) || health.pendingEvents < 0) {
    throw new Error('pendingEvents deve ser um inteiro não negativo durante offline.');
  }

  return health;
}

async function main() {
  const health = await runChaosOfflineCheck({ edgeUrl: process.env.CHAOS_EDGE_URL });
  console.log(
    `Caos offline OK: edge operacional, sync=${health.sync}, pendentes=${health.pendingEvents}.`,
  );
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch((error) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}
