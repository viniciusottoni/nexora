import { describe, expect, it, vi } from 'vitest';
import { PricingApi } from './pricing-api.js';

const variantId = '0198aabb-1111-7000-8000-000000000001';
const productId = '0198aabb-1111-7000-8000-000000000002';
const categoryId = '0198aabb-1111-7000-8000-000000000003';
const channels = ['DineIn', 'Delivery', 'Takeout', 'Marketplace'].map((channel) => ({
  channel,
  amount: '45.00',
  isInherited: channel !== 'DineIn',
  validFrom: '2026-08-02T20:00:00.000Z',
}));

describe('PricingApi', () => {
  it('consulta, salva e reajusta preços usando idempotência nas escritas', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, _init?: RequestInit) => {
      const url =
        typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
      return json(
        url.endsWith('/bulk-adjust')
          ? { updated: 3, effectiveFrom: '2026-08-02T20:00:00.000Z' }
          : { variantId, productId, channels },
      );
    });
    const api = new PricingApi('/api', fetcher);

    await expect(api.getPriceTable(variantId)).resolves.toMatchObject({ variantId });
    await expect(
      api.setPriceTable(variantId, { prices: [{ channel: 'Delivery', amount: '49.00' }] }),
    ).resolves.toMatchObject({ variantId });
    await expect(api.bulkAdjust({ categoryId, channel: 'DineIn', percent: 8 })).resolves.toEqual({
      updated: 3,
      effectiveFrom: '2026-08-02T20:00:00.000Z',
    });

    expect(fetcher).toHaveBeenCalledTimes(3);
    expect(
      fetcher.mock.calls
        .slice(1)
        .every((call) => new Headers(call[1]?.headers).has('Idempotency-Key')),
    ).toBe(true);
  });

  it('usa mensagem de fallback quando a falha não traz problem details', async () => {
    const api = new PricingApi('/api', async () => new Response(null, { status: 503 }));

    await expect(api.getPriceTable(variantId)).rejects.toThrow(
      'Não foi possível concluir a operação.',
    );
  });
});

function json(value: unknown): Response {
  return new Response(JSON.stringify(value), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}
