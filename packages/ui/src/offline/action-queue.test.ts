// IndexedDB não existe em vitest/jsdom nem no ambiente 'node' padrão deste repo (vitest.config.ts)
// — este polyfill dá ao Dexie uma implementação real de IndexedDB em memória, só para este arquivo
// (nenhum outro teste do repo precisa dele: os testes de `PosOrderCompositionApi`/
// `PublicOrderCompositionApi` injetam um duplo de `OfflineActionQueue`, mesmo padrão de
// `TableMapHubConnection` — não abrem Dexie de verdade).
import 'fake-indexeddb/auto';
import { describe, expect, it, vi } from 'vitest';
import { ActionQueue, isNetworkFailure } from './action-queue.js';

interface OrderPayload {
  readonly sessionId: string;
}

let dbCounter = 0;
/** Nome de banco único por teste — evita um teste ver dado deixado por outro (mesmo Dexie global). */
function uniqueDbName(): string {
  dbCounter += 1;
  return `test-action-queue-${dbCounter}`;
}

describe('ActionQueue (US-034 §8 — fila de ações do cliente)', () => {
  it('enfileira e reflete no count()', async () => {
    const send = vi.fn().mockResolvedValue(undefined);
    const queue = new ActionQueue<OrderPayload>({ dbName: uniqueDbName(), send });

    expect(await queue.count()).toBe(0);
    await queue.enqueue('order.create', { sessionId: 'mesa-1' }, 'idem-1');
    expect(await queue.count()).toBe(1);
  });

  it('drena com sucesso e remove da fila (count volta a 0)', async () => {
    const send = vi.fn().mockResolvedValue(undefined);
    const queue = new ActionQueue<OrderPayload>({ dbName: uniqueDbName(), send });

    await queue.enqueue('order.create', { sessionId: 'mesa-1' }, 'idem-1');
    await queue.flush();

    expect(send).toHaveBeenCalledWith('order.create', { sessionId: 'mesa-1' }, 'idem-1', expect.any(String));
    expect(await queue.count()).toBe(0);
  });

  it('preserva a MESMA Idempotency-Key entre tentativas, mesmo depois de falhar N vezes (ADR-020)', async () => {
    const send = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(undefined);
    const queue = new ActionQueue<OrderPayload>({ dbName: uniqueDbName(), send });

    await queue.enqueue('order.create', { sessionId: 'mesa-9' }, 'idem-fixa');

    await queue.flush(); // 1ª tentativa: falha, continua na fila
    expect(await queue.count()).toBe(1);
    await queue.flush(); // 2ª tentativa: falha, continua na fila
    expect(await queue.count()).toBe(1);
    await queue.flush(); // 3ª tentativa: sucesso, sai da fila
    expect(await queue.count()).toBe(0);

    expect(send).toHaveBeenCalledTimes(3);
    for (const call of send.mock.calls) {
      expect(call[2]).toBe('idem-fixa'); // nunca gera uma chave nova para a mesma intenção
    }
  });

  it('reenvia em ordem de occurredAt, não na ordem em que o reenvio aconteceu', async () => {
    const sentOrder: string[] = [];
    const send = vi.fn().mockImplementation(async (_action: string, payload: OrderPayload) => {
      sentOrder.push(payload.sessionId);
    });
    const queue = new ActionQueue<OrderPayload>({ dbName: uniqueDbName(), send });

    await queue.enqueue('order.create', { sessionId: 'segunda' }, 'idem-2', '2026-08-03T10:01:00.000Z');
    await queue.enqueue('order.create', { sessionId: 'primeira' }, 'idem-1', '2026-08-03T10:00:00.000Z');

    await queue.flush();

    expect(sentOrder).toEqual(['primeira', 'segunda']);
  });

  it('não duplica reenvio quando flush() é chamado duas vezes em paralelo (evento online + reconexão simultâneos)', async () => {
    let resolveSend: (() => void) | undefined;
    const send = vi.fn().mockImplementation(
      () =>
        new Promise<void>((resolve) => {
          resolveSend = resolve;
        }),
    );
    const queue = new ActionQueue<OrderPayload>({ dbName: uniqueDbName(), send });
    await queue.enqueue('order.create', { sessionId: 'mesa-1' }, 'idem-1');

    const firstFlush = queue.flush();
    const secondFlush = queue.flush(); // concorrente — deve ser ignorado (trava `flushing`)

    // `flush()` faz sua primeira leitura do IndexedDB de forma assíncrona (mesmo com o polyfill em
    // memória) — espera essa volta antes de checar quantas vezes `send` já foi chamado.
    await vi.waitFor(() => expect(send).toHaveBeenCalledTimes(1));
    resolveSend?.();
    await Promise.all([firstFlush, secondFlush]);

    expect(send).toHaveBeenCalledTimes(1);
  });

  describe('isNetworkFailure', () => {
    it('reconhece TypeError (fetch nem voltou Response) como falha de rede', () => {
      expect(isNetworkFailure(new TypeError('Failed to fetch'))).toBe(true);
    });

    it('reconhece AbortError (timeout de AbortController) como falha de rede', () => {
      expect(isNetworkFailure(new DOMException('aborted', 'AbortError'))).toBe(true);
    });

    it('NÃO trata um erro de negócio comum (resposta HTTP de erro já convertida) como falha de rede', () => {
      expect(isNetworkFailure(new Error('Produto indisponível'))).toBe(false);
    });
  });
});
