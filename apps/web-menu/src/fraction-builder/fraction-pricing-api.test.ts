// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest';
import { FractionPricingApi } from './fraction-pricing-api.js';

const response = {
  unitPrice: 52,
  priceRule: 'HIGHEST',
  description: 'G · Mussarela / Calabresa',
  fractions: [],
};

describe('FractionPricingApi', () => {
  afterEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
  });

  it('usa cliente público sem enviar token de gestão', async () => {
    localStorage.setItem('food-operations.cloud.access', 'token-secreto');
    const fetcher = vi.fn<typeof fetch>(
      async () => new Response(JSON.stringify(response), { status: 200 }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = new FractionPricingApi();
    await api.preview({
      fractions: [
        { variantId: '0198aabb-6666-7000-8000-000000000001', weight: 0.5 },
        { variantId: '0198aabb-6666-7000-8000-000000000002', weight: 0.5 },
      ],
    });

    const init = fetcher.mock.calls[0]![1]!;
    expect(new Headers(init.headers).has('Authorization')).toBe(false);
  });
});
