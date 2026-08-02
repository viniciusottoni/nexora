import { describe, expect, it, vi } from 'vitest';
import { CategoriesApi } from './categories-api.js';

const category = {
  id: '0198aabb-3333-7000-8000-000000000001',
  name: 'Pizzas Salgadas',
  description: 'Sabores tradicionais',
  position: 0,
  isActive: true,
  productCount: 0,
};

describe('CategoriesApi', () => {
  it('valida a resposta da listagem', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ items: [category] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new CategoriesApi('/api', fetcher);

    await expect(api.list()).resolves.toMatchObject({ items: [{ name: 'Pizzas Salgadas' }] });
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/catalog/categories');
  });

  it('envia uma Idempotency-Key nova em cada escrita', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(category), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new CategoriesApi('/api', fetcher);

    await api.create({ name: 'Pizzas Salgadas', position: 0 });
    await api.update(category.id, { position: 1 });

    const keys = fetcher.mock.calls.map((call) =>
      new Headers(call[1]?.headers).get('Idempotency-Key'),
    );
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBeTruthy();
    expect(keys[0]).not.toBe(keys[1]);
  });

  it('reorder chama PATCH /reorder com o corpo enviado', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }),
    );
    const api = new CategoriesApi('/api', fetcher);

    await api.reorder({ order: [category.id] });

    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/catalog/categories/reorder');
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('PATCH');
  });

  it('deactivate chama DELETE no id informado', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }),
    );
    const api = new CategoriesApi('/api', fetcher);

    await api.deactivate(category.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/catalog/categories/${category.id}`);
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('DELETE');
  });

  it('propaga a mensagem de erro do problem details em falha', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Categoria não encontrada.' }), {
          status: 404,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new CategoriesApi('/api', fetcher);

    await expect(api.update(category.id, { position: 1 })).rejects.toThrow(
      'Categoria não encontrada.',
    );
  });
});
