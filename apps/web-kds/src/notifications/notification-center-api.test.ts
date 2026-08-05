import { describe, expect, it, vi } from 'vitest';
import { NotificationCenterApi } from './notification-center-api.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return {
    ok,
    status,
    json: () => Promise.resolve(body),
  } as unknown as Response;
}

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

function alertFixture(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: '0198aabb-1111-7000-8000-000000000001',
    type: 'ORDER_LATE',
    severity: 'HIGH',
    entityType: 'ORDER',
    entityId: '0198aabb-1111-7000-8000-000000000002',
    message: 'Pedido A47 da mesa 12 está há 21 minutos na fila.',
    raisedAt: '2026-08-04T12:00:00.000Z',
    acknowledgedAt: null,
    acknowledgedBy: null,
    resolvedAt: null,
    targetRoles: ['WAITER'],
    targetUserId: null,
    groupKey: null,
    ...overrides,
  };
}

describe('NotificationCenterApi (US-081/US-083)', () => {
  it('busca as notificações não lidas com Bearer + credenciais de dispositivo', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ alerts: [alertFixture()], nextCursor: null }));
    const api = new NotificationCenterApi('', fetcher);

    const result = await api.listUnread(identity);

    expect(fetcher).toHaveBeenCalledOnce();
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/notifications?status=unread');
    const headers = new Headers(init.headers);
    expect(headers.get('Authorization')).toBe('Bearer token-abc');
    expect(headers.get('X-Device-Id')).toBe('device-1');
    expect(result.alerts).toHaveLength(1);
  });

  it('lança erro com a mensagem do problema RFC 7807 quando a resposta não é ok', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ detail: 'Sessão expirada.' }, false, 401));
    const api = new NotificationCenterApi('', fetcher);

    await expect(api.listUnread(identity)).rejects.toThrow('Sessão expirada.');
  });

  it('reconhece um alerta com Idempotency-Key e devolve o alerta atualizado', async () => {
    const acknowledged = alertFixture({ acknowledgedAt: '2026-08-04T12:05:00.000Z', acknowledgedBy: 'user-1' });
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(requestUrl(input)).toBe('/v1/alerts/0198aabb-1111-7000-8000-000000000001/acknowledge');
      expect(init?.method).toBe('POST');
      expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
      return jsonResponse(acknowledged);
    });
    const api = new NotificationCenterApi('', fetcher);

    const result = await api.acknowledge(identity, '0198aabb-1111-7000-8000-000000000001');

    expect(result.acknowledgedAt).toBe('2026-08-04T12:05:00.000Z');
  });
});
