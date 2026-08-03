import { describe, expect, it, vi } from 'vitest';
import { PosApiError, PosTablesApi } from './tables-api.js';

const identity = {
  accessToken: 'access-local',
  deviceId: '0198aabb-1111-7000-8000-000000000001',
  deviceSecret: 'segredo-local',
};

const tableId = '0198aabb-2222-7000-8000-000000000002';
const otherTableId = '0198aabb-2222-7000-8000-000000000004';

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

const noFlags = { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false };

describe('PosTablesApi', () => {
  it('lista só as mesas livres (o mapa completo é US-023, GET /v1/tables)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        tables: [
          { id: tableId, label: '1', area: 'Salão', status: 'FREE', seats: 4, session: null, flags: noFlags },
          {
            id: otherTableId,
            label: '2',
            area: 'Salão',
            status: 'OCCUPIED',
            seats: 4,
            session: {
              openedAt: new Date().toISOString(),
              minutesOpen: 5,
              total: '10.00',
              guestCount: 2,
              waiter: null,
              sessionId: '0198aabb-1111-7000-8000-000000000010',
            },
            flags: noFlags,
          },
        ],
      }),
    );
    const api = new PosTablesApi(identity, '', fetcher as unknown as typeof fetch);

    const result = await api.listFreeTables();

    expect(result).toHaveLength(1);
    expect(result[0]?.id).toBe(tableId);
  });

  it('envia Idempotency-Key e X-Occurred-At ao abrir uma mesa (RN-020, ADR-020)', async () => {
    let receivedInit: RequestInit | undefined;
    const fetcher = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      receivedInit = init;
      return jsonResponse(
        {
          id: '0198aabb-4444-7000-8000-000000000001',
          tableId,
          tableLabel: '1',
          status: 'OPEN',
          openedAt: new Date().toISOString(),
          guestCount: 4,
          guestCountConfirmed: true,
          waiterId: null,
          source: 'WAITER',
          currentItems: [],
          total: '0.00',
        },
        201,
      );
    });
    const api = new PosTablesApi(identity, '', fetcher as unknown as typeof fetch);

    const session = await api.openSession(tableId, { guestCount: 4 });

    expect(session.status).toBe('OPEN');
    expect(session.source).toBe('WAITER');
    const headers = new Headers(receivedInit?.headers);
    expect(headers.get('Idempotency-Key')).toBeTruthy();
    expect(headers.get('X-Occurred-At')).toBeTruthy();
    expect(receivedInit?.method).toBe('POST');
  });

  it('propaga o código de erro estável quando a mesa já tem sessão aberta (409)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ detail: 'Esta mesa já tem uma comanda em aberto.', code: 'TABLE_ALREADY_OPEN', meta: { sessionId: 'abc' } }, 409),
    );
    const api = new PosTablesApi(identity, '', fetcher as unknown as typeof fetch);

    await expect(api.openSession(tableId, { guestCount: 2 })).rejects.toMatchObject({
      code: 'TABLE_ALREADY_OPEN',
    });
  });

  it('devolve mensagem generica quando o corpo do erro nao e JSON valido', async () => {
    const fetcher = vi.fn(async () => new Response('erro interno', { status: 500 }));
    const api = new PosTablesApi(identity, '', fetcher as unknown as typeof fetch);

    await expect(api.openSession(tableId, { guestCount: 2 })).rejects.toBeInstanceOf(PosApiError);
  });
});
