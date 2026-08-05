import { describe, expect, it, vi } from 'vitest';
import { TableMapApi } from './table-map-api.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return {
    ok,
    status,
    json: () => Promise.resolve(body),
  } as unknown as Response;
}

describe('TableMapApi', () => {
  it('monta a query string de mine/sortBy e autentica com Bearer + credenciais de dispositivo', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ tables: [] }));
    const api = new TableMapApi('', fetcher);

    await api.list(identity, { mine: true, sortBy: 'label' });

    expect(fetcher).toHaveBeenCalledOnce();
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/tables?mine=true&sortBy=label');
    const headers = new Headers(init.headers);
    expect(headers.get('Authorization')).toBe('Bearer token-abc');
    expect(headers.get('X-Device-Id')).toBe('device-1');
    expect(headers.get('X-Device-Secret')).toBe('secret-1');
  });

  it('não anexa query string quando nenhum filtro é passado', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ tables: [] }));
    const api = new TableMapApi('', fetcher);

    await api.list(identity);

    const [url] = fetcher.mock.calls[0] as [string];
    expect(url).toBe('/v1/tables');
  });

  it('valida a resposta contra o contrato e devolve as mesas tipadas', async () => {
    const table = {
      id: '0198aabb-1111-7000-8000-000000000001',
      label: '12',
      area: 'Salão',
      status: 'OCCUPIED',
      seats: 4,
      session: {
        openedAt: '2026-08-02T18:00:00.000Z',
        minutesOpen: 47,
        total: '187.00',
        guestCount: 4,
        waiter: null,
        sessionId: '0198aabb-1111-7000-8000-000000000009',
      },
      flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false },
    };
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ tables: [table] }));
    const api = new TableMapApi('', fetcher);

    const result = await api.list(identity);

    expect(result.tables).toEqual([table]);
  });

  it('lança erro com a mensagem do problema RFC 7807 quando a resposta não é ok', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValue(jsonResponse({ detail: 'Não foi possível identificar o estabelecimento.' }, false, 403));
    const api = new TableMapApi('', fetcher);

    await expect(api.list(identity)).rejects.toThrow('Não foi possível identificar o estabelecimento.');
  });

  it('US-025 §7: confirma o atendimento com Idempotency-Key e devolve resolved/responseSeconds', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(input).toBe('/v1/tables/mesa-12/acknowledge-call');
      expect(init?.method).toBe('POST');
      expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
      return jsonResponse({ resolved: true, responseSeconds: 12 });
    });
    const api = new TableMapApi('', fetcher);

    const result = await api.acknowledgeWaiterCall(identity, 'mesa-12');

    expect(result).toEqual({ resolved: true, responseSeconds: 12 });
  });

  it('US-026 §4: pede a conta pelo garçom enviando splitMode no corpo', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(input).toBe('/v1/sessions/sessao-1/request-bill');
      expect(JSON.parse(init?.body as string)).toEqual({ splitMode: 'SINGLE' });
      return jsonResponse({
        session: {
          id: '0198aabb-1111-7000-8000-000000000001',
          tableId: '0198aabb-1111-7000-8000-000000000002',
          tableLabel: '12',
          status: 'BILLREQUESTED',
          openedAt: new Date().toISOString(),
          guestCount: 2,
          guestCountConfirmed: true,
          waiterId: null,
          source: 'WAITER',
          currentItems: [],
          total: '0.00',
        },
        alreadyRequested: false,
      });
    });
    const api = new TableMapApi('', fetcher);

    const result = await api.requestBill(identity, 'sessao-1', 'SINGLE');

    expect(result.session.status).toBe('BILLREQUESTED');
    expect(result.alreadyRequested).toBe(false);
  });
});
