import { describe, expect, it, vi } from 'vitest';
import { PublicTableApi, PublicTableApiError } from './public-table-api.js';

const tableId = '0198aabb-2222-7000-8000-000000000002';
const sessionId = '0198aabb-3333-7000-8000-000000000003';

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

async function captureError(action: () => Promise<unknown>) {
  try {
    await action();
    return undefined;
  } catch (cause: unknown) {
    return cause;
  }
}

describe('PublicTableApi', () => {
  it('resolve a mesa pelo qrToken e devolve o sessionToken anonimo', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        table: { id: tableId, label: '12', areaName: 'Salão' },
        session: {
          id: sessionId,
          tableId,
          tableLabel: '12',
          status: 'OPEN',
          openedAt: new Date().toISOString(),
          guestCount: 1,
          guestCountConfirmed: false,
          waiterId: null,
          source: 'QR',
          currentItems: [],
          total: '0.00',
        },
        sessionToken: 'jwt-de-sessao',
      }),
    );
    const api = new PublicTableApi('', fetcher);

    const result = await api.accessTable('token-mesa-12');

    expect(result.sessionToken).toBe('jwt-de-sessao');
    expect(result.session.source).toBe('QR');
    expect(result.session.guestCountConfirmed).toBe(false);
  });

  it('propaga INVALID_TABLE_TOKEN sem vazar detalhe de outra mesa', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ detail: 'Não conseguimos reconhecer esta mesa.', code: 'INVALID_TABLE_TOKEN' }, 404),
    );
    const api = new PublicTableApi('', fetcher);

    const error = await captureError(() => api.accessTable('token-que-nao-existe'));

    expect(error).toBeInstanceOf(PublicTableApiError);
    expect((error as PublicTableApiError).code).toBe('INVALID_TABLE_TOKEN');
  });

  it('busca o cardapio do canal DINE_IN', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      expect(requestUrl(input)).toContain('channel=DINE_IN');
      return jsonResponse({ tenantId: sessionId, tenantName: 'Dona Betinha', categories: [] });
    });
    const api = new PublicTableApi('', fetcher);

    const menu = await api.getMenu();

    expect(menu.tenantName).toBe('Dona Betinha');
  });
});
