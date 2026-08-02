import { performance } from 'node:perf_hooks';
import { pathToFileURL } from 'node:url';

function percentile95(values) {
  const ordered = [...values].sort((left, right) => left - right);
  return ordered[Math.max(0, Math.ceil(ordered.length * 0.95) - 1)] ?? 0;
}

export async function runLoadSmoke({
  baseUrl,
  path = '/v1/health',
  requests = 50,
  concurrency = 10,
  maxP95Ms = 3_000,
  fetchImpl = fetch,
}) {
  if (!baseUrl) throw new Error('LOAD_BASE_URL é obrigatório para o smoke de carga.');
  if (!Number.isInteger(requests) || requests < 1)
    throw new Error('LOAD_REQUESTS deve ser positivo.');
  if (!Number.isInteger(concurrency) || concurrency < 1) {
    throw new Error('LOAD_CONCURRENCY deve ser positivo.');
  }

  const url = new URL(path, baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`);
  const durations = [];
  let nextRequest = 0;
  let successful = 0;
  let failed = 0;

  async function worker() {
    while (nextRequest < requests) {
      nextRequest += 1;
      const startedAt = performance.now();
      try {
        const response = await fetchImpl(url, { signal: AbortSignal.timeout(maxP95Ms * 2) });
        if (!response.ok) failed += 1;
        else successful += 1;
      } catch {
        failed += 1;
      } finally {
        durations.push(performance.now() - startedAt);
      }
    }
  }

  await Promise.all(Array.from({ length: Math.min(concurrency, requests) }, () => worker()));
  const p95Ms = percentile95(durations);
  if (failed > 0)
    throw new Error(`Smoke de carga teve ${failed}/${requests} requisições com erro.`);
  if (p95Ms > maxP95Ms) {
    throw new Error(`Smoke de carga excedeu p95: ${p95Ms.toFixed(1)}ms > ${maxP95Ms}ms.`);
  }
  return { successful, failed, p95Ms };
}

async function main() {
  const result = await runLoadSmoke({
    baseUrl: process.env.LOAD_BASE_URL,
    path: process.env.LOAD_PATH ?? '/v1/health',
    requests: Number(process.env.LOAD_REQUESTS ?? 50),
    concurrency: Number(process.env.LOAD_CONCURRENCY ?? 10),
    maxP95Ms: Number(process.env.LOAD_MAX_P95_MS ?? 3_000),
  });
  console.log(
    `Carga smoke OK: ${result.successful} respostas, zero erro, p95=${result.p95Ms.toFixed(1)}ms.`,
  );
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch((error) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}
