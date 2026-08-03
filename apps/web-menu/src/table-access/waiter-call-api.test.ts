import { describe, expect, it, vi } from 'vitest';
import { WaiterCallApi, WaiterCallApiError } from './waiter-call-api.js';

const sessionId = '0198aabb-3333-7000-8000-000000000003';
const tableId = '0198aabb-2222-7000-8000-000000000002';

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

describe('WaiterCallApi', () => {
  it('chama o garcom anexando o Bearer do sessionToken e uma Idempotency-Key', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(input.toString()).toContain('/v1/public/table/token-mesa-12/call-waiter');
      const headers = init?.headers as Record<string, string>;
      expect(headers.Authorization).toBe('Bearer token-de-sessao');
      expect(headers['Idempotency-Key']).toBeTruthy();
      return jsonResponse({ acknowledged: true, alreadyPending: false });
    });
    const api = new WaiterCallApi('token-mesa-12', 'token-de-sessao', '', fetcher as unknown as typeof fetch);

    const result = await api.callWaiter();

    expect(result.acknowledged).toBe(true);
    expect(result.alreadyPending).toBe(false);
  });

  it('cenario "chamada repetida": devolve alreadyPending sem lancar erro', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ acknowledged: true, alreadyPending: true }));
    const api = new WaiterCallApi('token-mesa-12', 'token-de-sessao', '', fetcher as unknown as typeof fetch);

    const result = await api.callWaiter();

    expect(result.alreadyPending).toBe(true);
  });

  it('propaga o codigo de erro sem vazar detalhe (RN-015)', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ detail: 'Não conseguimos reconhecer esta mesa.', code: 'INVALID_TABLE_TOKEN' }, 404));
    const api = new WaiterCallApi('token-invalido', 'token-de-sessao', '', fetcher as unknown as typeof fetch);

    const error = await api.callWaiter().catch((cause) => cause);

    expect(error).toBeInstanceOf(WaiterCallApiError);
    expect((error as WaiterCallApiError).code).toBe('INVALID_TABLE_TOKEN');
  });

  it('pede a conta enviando splitMode e people no corpo', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(input.toString()).toContain('/request-bill');
      const body = JSON.parse(init?.body as string) as { splitMode: string; people?: number };
      expect(body).toEqual({ splitMode: 'BY_PERSON', people: 4 });
      return jsonResponse({
        session: {
          id: sessionId,
          tableId,
          tableLabel: '12',
          status: 'BILLREQUESTED',
          openedAt: new Date().toISOString(),
          guestCount: 4,
          guestCountConfirmed: true,
          waiterId: null,
          source: 'QR',
          currentItems: [],
          total: '0.00',
          splitMode: 'BY_PERSON',
          splitPeople: 4,
        },
        alreadyRequested: false,
      });
    });
    const api = new WaiterCallApi('token-mesa-12', 'token-de-sessao', '', fetcher as unknown as typeof fetch);

    const result = await api.requestBill('BY_PERSON', 4);

    expect(result.session.status).toBe('BILLREQUESTED');
    expect(result.session.splitPeople).toBe(4);
    expect(result.alreadyRequested).toBe(false);
  });

  it('pede a conta com modo SINGLE sem enviar people', async () => {
    const fetcher = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      const body = JSON.parse(init?.body as string) as Record<string, unknown>;
      expect(body).toEqual({ splitMode: 'SINGLE' });
      return jsonResponse({
        session: {
          id: sessionId,
          tableId,
          tableLabel: '12',
          status: 'BILLREQUESTED',
          openedAt: new Date().toISOString(),
          guestCount: 1,
          guestCountConfirmed: true,
          waiterId: null,
          source: 'QR',
          currentItems: [],
          total: '0.00',
        },
        alreadyRequested: false,
      });
    });
    const api = new WaiterCallApi('token-mesa-12', 'token-de-sessao', '', fetcher as unknown as typeof fetch);

    await api.requestBill('SINGLE');

    expect(fetcher).toHaveBeenCalledTimes(1);
  });
});
