import { describe, expect, it, vi } from 'vitest';
import { BillingApi, BillingApiError } from './billing-api.js';

const identity = {
  accessToken: 'access-local',
  deviceId: '0198aabb-1111-7000-8000-000000000001',
  deviceSecret: 'segredo-local',
};

const billResponse = {
  items: [],
  subtotal: '100.00',
  serviceFee: '0.00',
  total: '100.00',
  splitMode: 'BY_PERSON',
  split: [
    { person: 1, amount: '33.34', serviceFeeAmount: '0.00', serviceFeeWaived: false },
    { person: 2, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
    { person: 3, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
  ],
  pendingItems: [],
  hasPendingItems: false,
  amountPaid: null,
  remainingAmount: null,
  unassignedItemIds: [],
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

describe('BillingApi', () => {
  it('monta a query string de GET /bill com split/people/waived', async () => {
    const fetcher = vi.fn(async () => jsonResponse(billResponse));
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    await api.getBill(identity, 'session-1', { split: 'BY_PERSON', people: 3, waived: [1, 2] });

    const [url] = fetcher.mock.calls[0] as unknown as [string];
    expect(url).toContain('/v1/sessions/session-1/bill?');
    expect(url).toContain('split=BY_PERSON');
    expect(url).toContain('people=3');
    expect(url).toContain('waived=1%2C2');
  });

  it('envia Idempotency-Key em assign-items', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ ...billResponse, splitMode: 'BY_ITEM' }));
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    await api.assignItems(identity, 'session-1', {
      assignments: [{ person: 1, itemIds: ['0198aabb-3333-7000-8000-000000000001'] }],
    });

    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain('/bill/assign-items');
    expect(init.method).toBe('POST');
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy();
  });

  it('propaga o código estável do erro (ADR-021) quando a divisão por item recusa item órfão', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ detail: 'Item não atribuído.', code: 'BILL_ITEM_NOT_ASSIGNED' }, 422),
    );
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    await expect(
      api.assignItems(identity, 'session-1', { assignments: [{ person: 1, itemIds: [] }] }),
    ).rejects.toMatchObject({ code: 'BILL_ITEM_NOT_ASSIGNED' });
  });

  it('registra pagamento parcial e devolve o saldo em aberto', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        paymentId: '0198aabb-4444-7000-8000-000000000001',
        amountPaid: '50.00',
        remainingAmount: '130.00',
        total: '180.00',
        sessionStatus: 'BILLREQUESTED',
      }),
    );
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    const response = await api.registerPartialPayment(identity, 'session-1', { amount: 50, method: 'CASH' });

    expect(response.remainingAmount).toBe('130.00');
    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain('/bill/partial-payment');
    expect(JSON.parse(init.body as string)).toMatchObject({ amount: 50, method: 'CASH' });
  });

  it('envia X-Authorization-Token no pagamento parcial quando informado (US-035 §10)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        paymentId: '0198aabb-4444-7000-8000-000000000003',
        amountPaid: '50.00',
        remainingAmount: '0.00',
        total: '50.00',
        sessionStatus: 'BILLREQUESTED',
      }),
    );
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    await api.registerPartialPayment(identity, 'session-1', { amount: 50, method: 'CASH', reason: 'Cliente desistiu' }, 'token-abc');

    const [, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(new Headers(init.headers).get('X-Authorization-Token')).toBe('token-abc');
  });

  it('propaga o código PENDING_ITEMS e a lista de itens pendentes (US-035 §7)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse(
        {
          detail: 'Há itens que ainda não foram entregues.',
          code: 'PENDING_ITEMS',
          meta: { pendingItems: [{ name: 'Petit Gateau', status: 'READY' }] },
        },
        422,
      ),
    );
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    await expect(api.registerPartialPayment(identity, 'session-1', { amount: 50, method: 'CASH' })).rejects.toMatchObject({
      code: 'PENDING_ITEMS',
      meta: { pendingItems: [{ name: 'Petit Gateau', status: 'READY' }] },
    });
  });

  it('autoriza o fechamento com item pendente via /v1/auth/authorize (US-035 §10)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        authorizationToken: 'token-autorizacao',
        expiresIn: 120,
        authorizedBy: { id: '0198aabb-7777-7000-8000-000000000001', name: 'Gerente Ana' },
      }),
    );
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    const grant = await api.authorizeCloseWithPending(identity, { sessionId: 'session-1', pin: '1234', reason: 'Cliente desistiu' });

    expect(grant.authorizationToken).toBe('token-autorizacao');
    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain('/v1/auth/authorize');
    expect(JSON.parse(init.body as string)).toMatchObject({
      action: 'CLOSE_WITH_PENDING',
      pin: '1234',
      context: { sessionId: 'session-1', reason: 'Cliente desistiu' },
    });
  });

  it('erro genérico vira BillingApiError com mensagem amigável', async () => {
    const fetcher = vi.fn(async () => jsonResponse({}, 500));
    const api = new BillingApi('', fetcher as unknown as typeof fetch);

    await expect(api.getBill(identity, 'session-1')).rejects.toBeInstanceOf(BillingApiError);
  });
});
