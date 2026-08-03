import { describe, expect, it, vi } from 'vitest';
import { KdsQueueApi } from './kds-queue-api.js';

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
      orderCode: 'A47',
      productName: 'Pizza Calabresa Grande',
      quantity: 1,
      modifiers: ['sem cebola'],
      notes: 'bem assada',
      status: 'QUEUED',
      placedAt: '2026-08-03T12:00:00.000Z',
      elapsedSeconds: 30,
      table: '12',
      channel: 'DineIn',
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
