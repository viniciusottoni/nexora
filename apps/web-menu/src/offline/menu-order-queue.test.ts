// Ver comentário equivalente em packages/ui/src/offline/action-queue.test.ts — só este arquivo
// (que exercita a fila REAL, com Dexie de verdade) precisa do polyfill; os testes de
// `PublicOrderCompositionApi` injetam um duplo de `OfflineActionQueue` e não abrem IndexedDB.
import 'fake-indexeddb/auto';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { configureMenuOrderQueue, menuOrderQueue } from './menu-order-queue.js';

const sessionToken = 'jwt-de-sessao';
const variantId = '0198aabb-3333-7000-8000-000000000003';
// `createPublicOrderRequestSchema` (packages/contracts) exige ao menos 1 item.
const oneItem = [{ variantId, quantity: 1 }];

function jsonResponse(body: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: () => Promise.resolve(body) };
}

function orderResponseBody() {
  return {
    order: {
      id: '0198aabb-1111-7000-8000-000000000001',
      shortCode: 'A47',
      status: 'PLACED',
      sessionId: '0198aabb-2222-7000-8000-000000000002',
      channel: 'DineIn',
      total: '45.90',
      placedAt: new Date().toISOString(),
      items: [],
    },
    promisedAt: new Date().toISOString(),
    estimatedMinutes: 15,
  };
}

describe('menuOrderQueue (US-034 §8 — fila única do web-menu)', () => {
  afterEach(async () => {
    vi.unstubAllGlobals();
    await menuOrderQueue.flush().catch(() => {});
  });

  // ANTES do teste que chama `configureMenuOrderQueue` — `runtime` é um singleton de módulo, ver
  // comentário equivalente em pos-order-queue.test.ts.
  it('mantém a ação na fila quando a sessão da mesa ainda não foi configurada', async () => {
    await menuOrderQueue.enqueue('order.create', { items: oneItem }, 'idem-sem-sessao');

    await menuOrderQueue.flush();

    expect(await menuOrderQueue.count()).toBe(1);

    // Limpa o estado antes do próximo teste — mesma fila real (dbName fixo).
    configureMenuOrderQueue(sessionToken, '');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(orderResponseBody(), 201)));
    await menuOrderQueue.flush();
    expect(await menuOrderQueue.count()).toBe(0);
  });

  it('reenvia POST /v1/public/orders com a MESMA Idempotency-Key e o Bearer da sessão', async () => {
    configureMenuOrderQueue(sessionToken, '');
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(orderResponseBody(), 201));
    vi.stubGlobal('fetch', fetchMock);

    await menuOrderQueue.enqueue('order.create', { items: oneItem }, 'idem-fixa-456');
    expect(await menuOrderQueue.count()).toBe(1);

    await menuOrderQueue.flush();

    expect(await menuOrderQueue.count()).toBe(0);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/public/orders');
    const headers = new Headers(init.headers);
    expect(headers.get('Idempotency-Key')).toBe('idem-fixa-456');
    expect(headers.get('Authorization')).toBe(`Bearer ${sessionToken}`);
  });
});
