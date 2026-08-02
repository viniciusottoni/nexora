import { describe, expect, it, vi } from 'vitest';
import { StationsApi, stationColorCssValue } from './stations-api.js';

const station = {
  id: '0198aabb-1111-7000-8000-000000000003',
  code: 'FORNO',
  name: 'Forno',
  color: 'red',
  capacitySlots: 5,
  isBottleneck: true,
  position: 1,
  isActive: true,
  linkedProductCount: 3,
};

describe('StationsApi', () => {
  it('lista, cria, atualiza e remove praças pelas rotas contratadas', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'DELETE') return new Response(null, { status: 204 });
      if (!init?.method) return json({ items: [station] });
      expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy();
      return json(station);
    });
    const api = new StationsApi('/api', fetcher);

    await expect(api.list()).resolves.toMatchObject({ items: [{ name: 'Forno' }] });
    await expect(
      api.create({ code: 'FORNO', name: 'Forno', isBottleneck: true, position: 1 }),
    ).resolves.toMatchObject({ code: 'FORNO' });
    await expect(api.update(station.id, { capacitySlots: 5 })).resolves.toMatchObject({
      capacitySlots: 5,
    });
    await expect(api.remove(station.id)).resolves.toBeUndefined();

    expect(
      fetcher.mock.calls.map((call) => {
        const input = call[0];
        return typeof input === 'string'
          ? input
          : input instanceof URL
            ? input.toString()
            : input.url;
      }),
    ).toEqual([
      '/api/v1/catalog/stations',
      '/api/v1/catalog/stations',
      `/api/v1/catalog/stations/${station.id}`,
      `/api/v1/catalog/stations/${station.id}`,
    ]);
  });

  it('resolve token semântico e usa fallback seguro', () => {
    expect(stationColorCssValue('red')).toBe('var(--nx-danger-500)');
    expect(stationColorCssValue('#fff')).toBe('var(--color-border)');
    expect(stationColorCssValue(null)).toBe('var(--color-border)');
  });

  it('propaga detalhe de erro e usa mensagem segura quando corpo não é JSON', async () => {
    const detailed = new StationsApi(
      '/api',
      vi.fn(async () => json({ detail: 'Praça vinculada.' }, 422)),
    );
    await expect(detailed.remove(station.id)).rejects.toThrow('Praça vinculada.');

    const generic = new StationsApi(
      '/api',
      vi.fn(async () => new Response('falha', { status: 500 })),
    );
    await expect(generic.list()).rejects.toThrow('Não foi possível concluir a operação.');
  });
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}
