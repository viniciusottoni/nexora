import { describe, expect, it, vi } from 'vitest';
import { ProductsApi } from './products-api.js';

const product = {
  id: '0198aabb-4444-7000-8000-000000000001',
  categoryId: '0198aabb-3333-7000-8000-000000000001',
  categoryName: 'Pizzas Salgadas',
  stationId: null,
  stationName: null,
  name: 'Pizza Mussarela',
  description: null,
  ingredientsText: null,
  allergens: [],
  imageUrl: null,
  position: 0,
  isActive: true,
  isAvailable: true,
  allowsFractions: false,
  maxFractions: 1,
};

describe('ProductsApi', () => {
  it('valida a resposta da listagem e aceita filtro por categoria', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ items: [product] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new ProductsApi('/api', fetcher);

    await expect(api.list(product.categoryId)).resolves.toMatchObject({
      items: [{ name: 'Pizza Mussarela' }],
    });
    expect(fetcher.mock.calls[0]?.[0]).toBe(
      `/api/v1/catalog/products?categoryId=${product.categoryId}`,
    );
  });

  it('envia uma Idempotency-Key nova em cada escrita', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(product), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new ProductsApi('/api', fetcher);

    await api.create({
      categoryId: product.categoryId,
      name: 'Pizza Mussarela',
      allowsFractions: false,
      maxFractions: 1,
      position: 0,
      isActive: true,
    });
    await api.update(product.id, { name: 'Pizza Mussarela Especial' });

    const keys = fetcher.mock.calls.map((call) =>
      new Headers(call[1]?.headers).get('Idempotency-Key'),
    );
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBeTruthy();
    expect(keys[0]).not.toBe(keys[1]);
  });

  it('activate/deactivate chamam os endpoints dedicados, distintos de indisponibilidade operacional', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(product), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new ProductsApi('/api', fetcher);

    await api.activate(product.id);
    await api.deactivate(product.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/catalog/products/${product.id}/activate`);
    expect(fetcher.mock.calls[1]?.[0]).toBe(`/api/v1/catalog/products/${product.id}/deactivate`);
  });

  it('uploadImage prepara, envia direto ao object storage e confirma o MediaAsset', async () => {
    const uploadUrl = 'https://bucket.example.com/upload?signed=1';
    const publicUrl = 'https://cdn.example.com/tenants/t/products/p/original.hash.jpg';

    const fetcher = vi.fn(async (input: RequestInfo | URL, _init?: RequestInit) => {
      const url = requestUrl(input);
      if (url.endsWith('/image')) {
        return new Response(
          JSON.stringify({ uploadUrl, publicUrl, expiresAt: new Date().toISOString() }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }
      if (url.endsWith('/image/confirm')) {
        return new Response(
          JSON.stringify({ mediaAssetId: '0198aabb-5555-7000-8000-000000000001', url: publicUrl }),
          {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          },
        );
      }
      throw new Error(`unexpected authenticated fetch to ${url}`);
    });

    const globalFetch = vi
      .spyOn(globalThis, 'fetch')
      .mockImplementation(async (input: RequestInfo | URL) => {
        expect(requestUrl(input)).toBe(uploadUrl);
        return new Response(null, { status: 200 });
      });

    const api = new ProductsApi('/api', fetcher);
    const blob = new Blob(['fake-image-bytes'], { type: 'image/jpeg' });

    const result = await api.uploadImage(product.id, blob, 'image/jpeg', {
      width: 800,
      height: 800,
    });

    expect(result.url).toBe(publicUrl);
    expect(globalFetch).toHaveBeenCalledTimes(1);
    expect(fetcher).toHaveBeenCalledTimes(2);

    const confirmCall = fetcher.mock.calls.find((call) =>
      requestUrl(call[0]).endsWith('/image/confirm'),
    );
    const confirmBodyRaw = confirmCall?.[1]?.body;
    if (typeof confirmBodyRaw !== 'string') throw new Error('corpo de confirmação ausente');
    const confirmBody = JSON.parse(confirmBodyRaw);
    expect(confirmBody.width).toBe(800);
    expect(confirmBody.height).toBe(800);
    expect(confirmBody.sha256).toMatch(/^[0-9a-f]{64}$/);

    globalFetch.mockRestore();
  });

  it('propaga a mensagem de erro do problem details em falha', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Produto não encontrado.' }), {
          status: 404,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new ProductsApi('/api', fetcher);

    await expect(api.activate(product.id)).rejects.toThrow('Produto não encontrado.');
  });
});

function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input;
  return input instanceof URL ? input.href : input.url;
}
