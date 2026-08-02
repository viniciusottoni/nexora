import { describe, expect, it, vi } from 'vitest';
import { AreasApi, TablesApi } from './tables-api.js';

const area = { id: '0198aabb-1111-7000-8000-000000000001', name: 'Salão', position: 0, active: true, tableCount: 0 };
const table = {
  id: '0198aabb-2222-7000-8000-000000000002',
  areaId: area.id,
  areaName: 'Salão',
  label: '12',
  seats: 4,
  status: 'FREE',
  active: true,
  sortOrder: 0,
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

/**
 * Constrói o mock a partir de uma fábrica (não de uma instância única) — o corpo de
 * `Response` só pode ser lido uma vez; testes que chamam a API mais de uma vez (ex.:
 * "Idempotency-Key nova em cada intenção") precisam de uma resposta nova por chamada.
 */
function fetcherReturning(factory: () => Response) {
  return vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => factory());
}

describe('AreasApi', () => {
  it('lista ambientes sem enviar tenant do navegador', async () => {
    const fetcher = fetcherReturning(() => jsonResponse({ items: [area] }));
    const api = new AreasApi('/api', fetcher);

    await expect(api.list()).resolves.toMatchObject({ items: [{ name: 'Salão' }] });
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/areas');
  });

  it('envia uma Idempotency-Key nova em cada intencao de escrita', async () => {
    const fetcher = fetcherReturning(() => jsonResponse(area));
    const api = new AreasApi('/api', fetcher);

    await api.create({ name: 'Salão', position: 0 });
    await api.update(area.id, { name: 'Varanda', position: 1 });

    const keys = fetcher.mock.calls.map((call) => new Headers(call[1]?.headers).get('Idempotency-Key'));
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBeTruthy();
    expect(keys[0]).not.toBe(keys[1]);
  });

  it('desativa e exclui via os endpoints dedicados', async () => {
    const fetcher = fetcherReturning(() => new Response(null, { status: 204 }));
    const api = new AreasApi('/api', fetcher);

    await api.deactivate(area.id);
    await api.remove(area.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/areas/${area.id}/deactivate`);
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(fetcher.mock.calls[1]?.[0]).toBe(`/api/v1/areas/${area.id}`);
    expect(fetcher.mock.calls[1]?.[1]?.method).toBe('DELETE');
  });
});

describe('TablesApi', () => {
  it('lista mesas filtrando por ambiente quando informado', async () => {
    const fetcher = fetcherReturning(() => jsonResponse({ items: [table] }));
    const api = new TablesApi('/api', fetcher);

    await api.list(area.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/tables?areaId=${area.id}`);
  });

  it('cria mesas em lote no endpoint dedicado', async () => {
    const fetcher = fetcherReturning(() => jsonResponse({ items: [table] }));
    const api = new TablesApi('/api', fetcher);

    await api.createBulk({ areaId: area.id, from: 1, to: 20, seats: 4 });

    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/tables/bulk');
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(JSON.parse(String(fetcher.mock.calls[0]?.[1]?.body))).toEqual({
      areaId: area.id,
      from: 1,
      to: 20,
      seats: 4,
    });
  });

  it('rotaciona o token da mesa sem esperar corpo de resposta', async () => {
    const fetcher = fetcherReturning(() => new Response(null, { status: 204 }));
    const api = new TablesApi('/api', fetcher);

    await api.rotateQrToken(table.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/tables/${table.id}/rotate-token`);
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('Idempotency-Key')).toBeTruthy();
  });

  it('exporta o PDF de QR Codes como blob, opcionalmente filtrado por ambiente', async () => {
    const pdfBytes = new Uint8Array([1, 2, 3]);
    const fetcher = fetcherReturning(
      () => new Response(pdfBytes, { status: 200, headers: { 'Content-Type': 'application/pdf' } }),
    );
    const api = new TablesApi('/api', fetcher);

    const blob = await api.exportQrCodesPdf(area.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/tables/qr-codes.pdf?areaId=${area.id}`);
    expect(blob.size).toBeGreaterThan(0);
  });

  it('propaga a mensagem em portugues do problem details em caso de erro', async () => {
    const fetcher = fetcherReturning(
      () =>
        new Response(JSON.stringify({ detail: 'Esta mesa tem sessões no histórico.' }), {
          status: 422,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
    );
    const api = new TablesApi('/api', fetcher);

    await expect(api.remove(table.id)).rejects.toThrow('Esta mesa tem sessões no histórico.');
  });
});
