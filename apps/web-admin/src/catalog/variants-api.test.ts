import { describe, expect, it, vi } from 'vitest';
import { VariantsApi } from './variants-api.js';

const variant = {
  id: '0198aabb-4444-7000-8000-000000000001',
  productId: '0198aabb-3333-7000-8000-000000000001',
  name: 'Grande',
  sku: null,
  sizeCode: 'G',
  prepMinutes: 10,
  isDefault: true,
  isActive: true,
  currentPrice: '45.90',
  currentPriceChannel: 'DineIn',
};

describe('VariantsApi', () => {
  it('lista variantes do produto e valida dinheiro como string', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ items: [variant] }), { status: 200 }),
    );
    const api = new VariantsApi('/api', fetcher);

    await expect(api.listForProduct(variant.productId)).resolves.toEqual({ items: [variant] });
    expect(fetcher.mock.calls[0]?.[0]).toBe(
      `/api/v1/catalog/products/${variant.productId}/variants`,
    );
  });

  it('envia preço base textual e Idempotency-Key ao criar', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(variant), { status: 200 }),
    );
    const api = new VariantsApi('/api', fetcher);

    await api.create(variant.productId, { name: 'Grande', basePrice: '45.90', isDefault: true });

    const requestBody = fetcher.mock.calls[0]?.[1]?.body;
    if (typeof requestBody !== 'string') throw new Error('corpo de criação ausente');
    expect(JSON.parse(requestBody)).toMatchObject({ basePrice: '45.90' });
    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('Idempotency-Key')).toBeTruthy();
  });

  it('propaga detail do Problem Details', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Variante não encontrada.' }), { status: 404 }),
    );
    const api = new VariantsApi('/api', fetcher);

    await expect(api.deactivate(variant.id)).rejects.toThrow('Variante não encontrada.');
  });
});
