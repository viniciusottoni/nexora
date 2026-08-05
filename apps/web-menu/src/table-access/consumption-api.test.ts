import { describe, expect, it, vi } from 'vitest';
import { ConsumptionApi, ConsumptionApiError } from './consumption-api.js';

const orderId = '0198aabb-4444-7000-8000-000000000004';
const itemId = '0198aabb-5555-7000-8000-000000000005';
const variantId = '0198aabb-6666-7000-8000-000000000006';

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

describe('ConsumptionApi', () => {
  it('busca o consumo atual anexando o Bearer do sessionToken', async () => {
    const fetcher = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      expect((init?.headers as Record<string, string>).Authorization).toBe('Bearer token-de-sessao');
      return jsonResponse({
        items: [
          {
            orderItemId: itemId,
            orderId,
            name: 'Pizza G Mussarela',
            quantity: 1,
            unitPrice: '52.00',
            total: '52.00',
            status: 'QUEUED',
            statusLabel: 'Na fila',
            etaMinutes: 10,
            cancelled: false,
            variantId,
            productAvailable: true,
          },
        ],
        subtotal: '52.00',
        serviceFee: '5.20',
        serviceFeeOptional: true,
        total: '57.20',
        openedAt: new Date().toISOString(),
        minutesOpen: 12,
      });
    });
    const api = new ConsumptionApi('token-de-sessao', '', fetcher);

    const result = await api.getCurrentConsumption();

    expect(result.items).toHaveLength(1);
    expect(result.items[0]?.statusLabel).toBe('Na fila');
    expect(result.serviceFeeOptional).toBe(true);
  });

  it('propaga o codigo de erro sem vazar detalhe (ex.: sessao nao encontrada, 404 nunca 403)', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ detail: 'Sessão não encontrada.', code: 'TABLE_SESSION_NOT_FOUND' }, 404));
    const api = new ConsumptionApi('token-de-outra-mesa', '', fetcher);

    const error = await captureError(() => api.getCurrentConsumption());

    expect(error).toBeInstanceOf(ConsumptionApiError);
    expect((error as ConsumptionApiError).code).toBe('TABLE_SESSION_NOT_FOUND');
  });

  it('repete um item enviando Idempotency-Key e devolve o preco vigente', async () => {
    const seenKeys = new Set<string>();
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(requestUrl(input)).toContain(`/v1/orders/${orderId}/items/${itemId}/repeat`);
      const key = (init?.headers as Record<string, string>)['Idempotency-Key'];
      expect(key).toBeTruthy();
      if (key) seenKeys.add(key);
      return jsonResponse({ item: { id: 'novo-item', unitPrice: '55.00', repeatedFromItemId: itemId } }, 201);
    });
    const api = new ConsumptionApi('token-de-sessao', '', fetcher);

    const result = await api.repeatItem(orderId, itemId);

    expect(result.unitPrice).toBe('55.00');
    expect(result.repeatedFrom).toBe(itemId);
    expect(seenKeys.size).toBe(1);
  });

  it('bloqueio de produto indisponivel propaga PRODUCT_UNAVAILABLE', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ detail: 'Produto indisponível.', code: 'PRODUCT_UNAVAILABLE' }, 422));
    const api = new ConsumptionApi('token-de-sessao', '', fetcher);

    const error = await captureError(() => api.repeatItem(orderId, itemId));

    expect(error).toBeInstanceOf(ConsumptionApiError);
    expect((error as ConsumptionApiError).code).toBe('PRODUCT_UNAVAILABLE');
  });

  it('busca a prévia da divisão da conta (US-027 §10) com split/people na query', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      expect(url).toContain('/v1/public/sessions/current/bill?');
      expect(url).toContain('split=BY_PERSON');
      expect(url).toContain('people=4');
      return jsonResponse({
        items: [],
        subtotal: '80.00',
        serviceFee: '0.00',
        total: '80.00',
        splitMode: 'BY_PERSON',
        split: [
          { person: 1, amount: '20.00', serviceFeeAmount: '0.00', serviceFeeWaived: false },
          { person: 2, amount: '20.00', serviceFeeAmount: '0.00', serviceFeeWaived: false },
          { person: 3, amount: '20.00', serviceFeeAmount: '0.00', serviceFeeWaived: false },
          { person: 4, amount: '20.00', serviceFeeAmount: '0.00', serviceFeeWaived: false },
        ],
        pendingItems: [],
        hasPendingItems: false,
        amountPaid: null,
        remainingAmount: null,
        unassignedItemIds: [],
      });
    });
    const api = new ConsumptionApi('token-de-sessao', '', fetcher);

    const bill = await api.getBillPreview('BY_PERSON', 4);

    expect(bill.split).toHaveLength(4);
    expect(bill.split.every((part) => part.amount === '20.00')).toBe(true);
  });
});
