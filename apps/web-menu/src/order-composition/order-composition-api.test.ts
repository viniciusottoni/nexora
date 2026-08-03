import { afterEach, describe, expect, it, vi } from 'vitest';
import type { OfflineActionQueue } from '@nexora/ui';
import type { QueuedOrderPayload } from '../offline/menu-order-queue.js';
import { PublicOrderCompositionApi } from './order-composition-api.js';

const sessionToken = 'jwt-de-sessao';
const variantId = '0198aabb-2222-7000-8000-000000000010';
const items = [{ variantId, quantity: 1 }];

/** Duplo de `OfflineActionQueue` — mesmo padrão de `TableMapHubConnection` (não abre IndexedDB aqui). */
function fakeQueue(): OfflineActionQueue<QueuedOrderPayload> & { enqueue: ReturnType<typeof vi.fn> } {
  return {
    enqueue: vi.fn().mockResolvedValue(undefined),
    flush: vi.fn().mockResolvedValue(undefined),
    count: vi.fn().mockResolvedValue(0),
  };
}

function jsonResponse(body: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: () => Promise.resolve(body) };
}

describe('PublicOrderCompositionApi.createOrder (US-034 §4/§7 — envio otimista na queda de LAN)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('retorna "sent" com o pedido do servidor no caminho feliz — sem tocar a fila', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse(
        {
          order: {
            id: '0198aabb-1111-7000-8000-000000000001',
            shortCode: 'A47',
            status: 'PLACED',
            sessionId: null,
            channel: 'DineIn',
            total: '45.90',
            placedAt: null,
            items: [],
          },
          promisedAt: '2026-08-03T20:15:00.000Z',
          estimatedMinutes: 15,
        },
        201,
      ),
    );
    vi.stubGlobal('fetch', fetchMock);
    const queue = fakeQueue();
    const api = new PublicOrderCompositionApi(sessionToken, '', fetchMock, queue);

    const outcome = await api.createOrder(items);

    expect(outcome.status).toBe('sent');
    if (outcome.status === 'sent') {
      expect(outcome.order.shortCode).toBe('A47');
    }
    expect(queue.enqueue).not.toHaveBeenCalled();
  });

  it('enfileira com a MESMA Idempotency-Key e retorna "queued" quando o fetch falha por REDE (TypeError)', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'));
    const queue = fakeQueue();
    const api = new PublicOrderCompositionApi(sessionToken, '', fetchMock, queue);

    const outcome = await api.createOrder(items);

    expect(outcome.status).toBe('queued');
    expect(queue.enqueue).toHaveBeenCalledTimes(1);
    const [action, payload, idempotencyKey] = queue.enqueue.mock.calls[0] as [string, QueuedOrderPayload, string];
    expect(action).toBe('order.create');
    expect(payload).toEqual({ items });
    if (outcome.status === 'queued') {
      expect(outcome.idempotencyKey).toBe(idempotencyKey);
    }
  });

  it('propaga o erro de negócio (resposta HTTP real) sem enfileirar nada — não é falha de rede', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ code: 'PRODUCT_UNAVAILABLE', detail: 'Produto indisponível.', meta: { itemIndex: 0 } }, 422),
    );
    const queue = fakeQueue();
    const api = new PublicOrderCompositionApi(sessionToken, '', fetchMock, queue);

    await expect(api.createOrder(items)).rejects.toMatchObject({ code: 'PRODUCT_UNAVAILABLE' });
    expect(queue.enqueue).not.toHaveBeenCalled();
  });
});
