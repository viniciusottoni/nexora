import { describe, expect, it, vi } from 'vitest';
import { KdsApiError, KdsQueueApi } from './kds-queue-api.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };
const stationId = '0198aabb-1111-7000-8000-000000000050';

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return {
    ok,
    status,
    json: () => Promise.resolve(body),
  } as unknown as Response;
}

describe('KdsQueueApi (US-031 §7)', () => {
  it('monta a query string com stationId e autentica com Bearer + credenciais de dispositivo', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ items: [], lastEventId: '2026-08-03T12:00:00.000Z' }));
    const api = new KdsQueueApi('', fetcher);

    await api.queue(identity, stationId);

    expect(fetcher).toHaveBeenCalledOnce();
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe(`/v1/kds/queue?stationId=${stationId}`);
    const headers = new Headers(init.headers);
    expect(headers.get('Authorization')).toBe('Bearer token-abc');
    expect(headers.get('X-Device-Id')).toBe('device-1');
  });

  it('anexa since quando informado (ADR-011, cursor de reconexão/polling)', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ items: [], lastEventId: '2026-08-03T12:00:05.000Z' }));
    const api = new KdsQueueApi('', fetcher);

    await api.queue(identity, stationId, '2026-08-03T12:00:00.000Z');

    const [url] = fetcher.mock.calls[0] as [string];
    expect(url).toBe(`/v1/kds/queue?stationId=${stationId}&since=2026-08-03T12%3A00%3A00.000Z`);
  });

  it('valida a resposta contra o contrato e devolve os itens tipados', async () => {
    const item = {
      orderItemId: '0198aabb-1111-7000-8000-000000000001',
      orderId: '0198aabb-1111-7000-8000-000000000099',
      orderCode: 'A47',
      productId: '0198aabb-1111-7000-8000-0000000000c1',
      productName: 'Pizza Calabresa Grande',
      quantity: 1,
      modifiers: ['sem cebola'],
      notes: 'bem assada',
      status: 'QUEUED',
      placedAt: '2026-08-03T12:00:00.000Z',
      elapsedSeconds: 30,
      thresholdState: 'NORMAL',
      warnSeconds: 720,
      criticalSeconds: 1080,
      table: '12',
      channel: 'DineIn',
      fractions: [],
    };
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ items: [item], lastEventId: '2026-08-03T12:00:30.000Z' }));
    const api = new KdsQueueApi('', fetcher);

    const result = await api.queue(identity, stationId);

    expect(result.items).toEqual([item]);
    expect(result.lastEventId).toBe('2026-08-03T12:00:30.000Z');
  });

  it('lança erro com a mensagem do problema RFC 7807 quando a resposta não é ok', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ detail: 'Praça não encontrada.' }, false, 404));
    const api = new KdsQueueApi('', fetcher);

    await expect(api.queue(identity, stationId)).rejects.toThrow('Praça não encontrada.');
  });
});

describe('KdsQueueApi.advanceOrder (US-041 §7)', () => {
  it('envia stationId e batch no corpo, com Idempotency-Key nova', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ advanced: [] }));
    const api = new KdsQueueApi('', fetcher);

    await api.advanceOrder(identity, '47', stationId, true);

    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/kds/orders/47/advance');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toEqual({ stationId, batch: true });
    const headers = new Headers(init.headers);
    expect(headers.get('Idempotency-Key')).toBeTruthy();
  });

  it('preserva o code RFC 7807 (ADR-021) em KdsApiError para o teclado distinguir código inexistente', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValue(jsonResponse({ detail: 'Nenhum pedido encontrado.', code: 'SHORT_CODE_NOT_FOUND' }, false, 404));
    const api = new KdsQueueApi('', fetcher);

    await expect(api.advanceOrder(identity, '999', stationId)).rejects.toMatchObject({
      code: 'SHORT_CODE_NOT_FOUND',
    } satisfies Partial<KdsApiError>);
  });
});

describe('KdsQueueApi.advanceItem / undoItem (US-041 §3/§7)', () => {
  const itemId = '0198aabb-1111-7000-8000-000000000001';

  it('advanceItem chama POST /v1/kds/items/{id}/advance', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      jsonResponse({
        id: itemId,
        orderId: stationId,
        variantId: stationId,
        name: 'Pizza',
        quantity: 1,
        unitPrice: '40.00',
        totalPrice: '40.00',
        status: 'FIRED',
        notes: null,
        stationId,
        repeatedFromItemId: null,
      }),
    );
    const api = new KdsQueueApi('', fetcher);

    const result = await api.advanceItem(identity, itemId);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/v1/kds/items/${itemId}/advance`);
    expect(result.status).toBe('FIRED');
  });

  it('undoItem chama POST /v1/kds/items/{id}/undo', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      jsonResponse({
        id: itemId,
        orderId: stationId,
        variantId: stationId,
        name: 'Pizza',
        quantity: 1,
        unitPrice: '40.00',
        totalPrice: '40.00',
        status: 'QUEUED',
        notes: null,
        stationId,
        repeatedFromItemId: null,
      }),
    );
    const api = new KdsQueueApi('', fetcher);

    const result = await api.undoItem(identity, itemId);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/v1/kds/items/${itemId}/undo`);
    expect(result.status).toBe('QUEUED');
  });
});
