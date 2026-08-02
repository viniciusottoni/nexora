import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { afterEach, test } from 'node:test';
import { runChaosOfflineCheck } from './chaos-offline.mjs';
import { runDataIntegrityCheck } from './data-integrity.mjs';
import { runLoadSmoke } from './load-smoke.mjs';
import { runMetricsRecalculationCheck } from './metrics-recalculation.mjs';

const servers = [];

async function listen(handler) {
  const server = createServer(handler);
  servers.push(server);
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const address = server.address();
  assert(address && typeof address === 'object');
  return `http://127.0.0.1:${address.port}`;
}

afterEach(async () => {
  await Promise.all(
    servers.splice(0).map((server) => new Promise((resolve) => server.close(resolve))),
  );
});

test('caos offline confirma edge operacional com sync degradado', async () => {
  const edgeUrl = await listen((_request, response) => {
    response.setHeader('content-type', 'application/json');
    response.end(
      JSON.stringify({
        postgres: 'OK',
        redis: 'OK',
        sync: 'DOWN',
        pendingEvents: 3,
        lastSyncAt: null,
        version: '0.1.0',
      }),
    );
  });

  const result = await runChaosOfflineCheck({ edgeUrl });

  assert.equal(result.sync, 'DOWN');
  assert.equal(result.pendingEvents, 3);
});

test('caos offline reprova edge parado ou falsamente online', async () => {
  const edgeUrl = await listen((_request, response) => {
    response.setHeader('content-type', 'application/json');
    response.end(JSON.stringify({ postgres: 'OK', redis: 'OK', sync: 'OK', pendingEvents: 0 }));
  });

  await assert.rejects(runChaosOfflineCheck({ edgeUrl }), /sync.*offline/i);
});

test('integridade de dados reprova qualquer invariável violada', async () => {
  const client = {
    async query() {
      return { rows: [{ check_name: 'rls_completo', failures: '1' }] };
    },
  };

  await assert.rejects(runDataIntegrityCheck(client), /rls_completo.*1/);
});

test('integridade de dados aceita todas as invariáveis com zero falhas', async () => {
  const client = {
    async query() {
      return {
        rows: [
          { check_name: 'rls_completo', failures: '0' },
          { check_name: 'eventos_duplicados', failures: '0' },
          { check_name: 'append_only', failures: '0' },
        ],
      };
    },
  };

  const result = await runDataIntegrityCheck(client);
  assert.equal(result.length, 3);
});

test('recálculo de métricas compara total bruto com buckets por occurred_at', async () => {
  const client = {
    async query() {
      return { rows: [{ raw_events: '7', bucketed_events: '7' }] };
    },
  };

  await assert.doesNotReject(runMetricsRecalculationCheck(client));
});

test('recálculo de métricas reprova divergência silenciosa', async () => {
  const client = {
    async query() {
      return { rows: [{ raw_events: '7', bucketed_events: '6' }] };
    },
  };

  await assert.rejects(runMetricsRecalculationCheck(client), /7.*6/);
});

test('smoke de carga mede p95 e exige zero erro', async () => {
  const baseUrl = await listen((_request, response) => {
    response.setHeader('content-type', 'application/json');
    response.end(JSON.stringify({ status: 'OK' }));
  });

  const result = await runLoadSmoke({ baseUrl, requests: 12, concurrency: 4, maxP95Ms: 1_000 });

  assert.equal(result.successful, 12);
  assert.equal(result.failed, 0);
  assert(result.p95Ms <= 1_000);
});
