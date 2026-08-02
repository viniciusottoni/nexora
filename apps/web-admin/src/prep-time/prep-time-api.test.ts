import { describe, expect, it, vi } from 'vitest';
import { PrepTimeApi } from './prep-time-api.js';

const variantId = '0198aabb-1111-7000-8000-000000000001';
const productId = '0198aabb-1111-7000-8000-000000000002';
const stationId = '0198aabb-1111-7000-8000-000000000003';

describe('PrepTimeApi', () => {
  it('atualiza tempo e praça com idempotência e contratos válidos', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url =
        typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
      expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
      if (url.endsWith('/prep-time')) {
        return json({ variantId, prepMinutes: 12, warnMinutes: 15, criticalMinutes: 20 });
      }
      return json({ productId, stationId, stationCode: 'FORNO', stationName: 'Forno' });
    });
    const api = new PrepTimeApi('/api', fetcher);

    await expect(
      api.updatePrepTime(variantId, { prepMinutes: 12, warnMinutes: 15, criticalMinutes: 20 }),
    ).resolves.toMatchObject({ prepMinutes: 12 });
    await expect(api.reassignStation(productId, stationId)).resolves.toMatchObject({
      stationName: 'Forno',
    });
  });

  it('carrega análise e propaga ProblemDetails', async () => {
    const fetcher = vi.fn(async () =>
      json({
        variantId,
        configuredMinutes: 12,
        effectiveWarnMinutes: 15,
        warnMinutesInherited: true,
        effectiveCriticalMinutes: 20,
        criticalMinutesInherited: true,
        actualAvgMinutes: 16.4,
        actualP90Minutes: null,
        sampleSize: 30,
        suggestion: 16,
        note: null,
      }),
    );
    const api = new PrepTimeApi('/api', fetcher);

    await expect(api.getPrepTimeAnalysis(variantId)).resolves.toMatchObject({ suggestion: 16 });

    const failingApi = new PrepTimeApi(
      '/api',
      vi.fn(async () => json({ detail: 'Variação não encontrada.' }, 404)),
    );
    await expect(failingApi.getPrepTimeAnalysis(variantId)).rejects.toThrow(
      'Variação não encontrada.',
    );
  });
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}
