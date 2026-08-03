// Ver comentário equivalente em packages/ui/src/offline/action-queue.test.ts — só este arquivo
// (que exercita a fila REAL, com Dexie de verdade) precisa do polyfill; os testes de
// `PosOrderCompositionApi` injetam um duplo de `OfflineActionQueue` e não abrem IndexedDB.
import 'fake-indexeddb/auto';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { configurePosOrderQueue, posOrderQueue } from './pos-order-queue.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };
const variantId = '0198aabb-3333-7000-8000-000000000003';
// `createOrderRequestSchema` (packages/contracts) exige ao menos 1 item — payload vazio nunca
// passaria da validação local, então nem chegaria a testar o comportamento de rede/fila.
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

describe('posOrderQueue (US-034 §8 — fila única do web-pos)', () => {
  afterEach(async () => {
    vi.unstubAllGlobals();
    // Esvazia a fila REAL entre testes — é um singleton de módulo (mesmo dbName sempre), então um
    // teste anterior não pode deixar sujeira para o próximo.
    await posOrderQueue.flush().catch(() => {});
  });

  // ANTES do teste que chama `configurePosOrderQueue` — `runtime` é um singleton de módulo, então
  // a ordem importa aqui: depois de configurada uma vez, a identidade fica setada para o resto do
  // arquivo (mesmo comportamento em produção, onde só é configurada de novo ao reautenticar).
  it('mantém a ação na fila quando a identidade ainda não foi configurada', async () => {
    // Simula um restart do dispositivo: a fila tem backlog de uma sessão anterior, mas o app
    // ainda não terminou de autenticar de novo.
    await posOrderQueue.enqueue('order.create', { sessionId: null, items: oneItem }, 'idem-sem-identidade');

    await posOrderQueue.flush();

    expect(await posOrderQueue.count()).toBe(1);

    // Limpa o estado antes do próximo teste — é a MESMA fila real (dbName fixo de propósito, ver
    // docstring de `posOrderQueue`), então uma sobra aqui contaminaria a contagem do teste seguinte.
    configurePosOrderQueue(identity, '');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(orderResponseBody(), 201)));
    await posOrderQueue.flush();
    expect(await posOrderQueue.count()).toBe(0);
  });

  it('reenvia POST /v1/orders com a MESMA Idempotency-Key e a identidade configurada', async () => {
    configurePosOrderQueue(identity, '');
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(orderResponseBody(), 201));
    vi.stubGlobal('fetch', fetchMock);

    await posOrderQueue.enqueue(
      'order.create',
      { sessionId: '0198aabb-2222-7000-8000-000000000002', items: oneItem },
      'idem-fixa-123',
    );
    expect(await posOrderQueue.count()).toBe(1);

    await posOrderQueue.flush();

    expect(await posOrderQueue.count()).toBe(0);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/orders');
    const headers = new Headers(init.headers);
    expect(headers.get('Idempotency-Key')).toBe('idem-fixa-123');
    expect(headers.get('Authorization')).toBe('Bearer token-abc');
    expect(headers.get('X-Device-Id')).toBe('device-1');
  });
});
