// Dexie v4 recomenda o import NOMEADO (não o default) para ESM — o default export tem histórico
// de problemas de interop CJS/ESM entre bundlers distintos (ver changelog do Dexie 4.x).
import { Dexie, type Table, type UpdateSpec } from 'dexie';

/**
 * Fila de ações do cliente (Dexie/IndexedDB) — US-034 §8: cobre a queda de LAN entre o
 * dispositivo e o edge server ("garçom cujo celular perdeu o Wi-Fi por 20 segundos"), um problema
 * DISTINTO da queda de internet entre o edge e a nuvem (esse é o `outbox` do backend — ver §8 da
 * história, "são dois problemas distintos com soluções distintas"). Cada ação guarda a MESMA
 * `idempotencyKey` gerada quando a intenção nasceu no cliente (ADR-020) — reenviar não duplica.
 */
export interface QueuedAction<TPayload = unknown> {
  readonly id: number;
  readonly action: string;
  readonly payload: TPayload;
  readonly idempotencyKey: string;
  /** ISO — RN-020: preserva o horário real da intenção mesmo que o envio só aconteça bem depois. */
  readonly occurredAt: string;
  readonly attempts: number;
}

/**
 * Superfície mínima que um cliente HTTP operacional precisa da fila — permite injetar um duplo de
 * teste (mesmo padrão de `TableMapHubConnection`/`KdsHubConnection`, ver
 * `apps/web-pos/src/table-map/table-map-realtime.ts`) em vez de abrir IndexedDB de verdade em
 * vitest/jsdom nos testes de `PosOrderCompositionApi`/`PublicOrderCompositionApi`.
 */
export interface OfflineActionQueue<TPayload = unknown> {
  enqueue(action: string, payload: TPayload, idempotencyKey: string, occurredAt?: string): Promise<void>;
  /** Reenvia tudo que estiver pendente, em ordem de `occurredAt`. Nunca lança — falhas ficam na fila. */
  flush(): Promise<void>;
  count(): Promise<number>;
}

export interface ActionQueueOptions<TPayload> {
  /** Nome do banco IndexedDB — FIXO por app (não por instância), para toda tela que crie um cliente novo ler/escrever a MESMA fila persistida. */
  readonly dbName: string;
  /** Reenvia uma ação — deve LANÇAR em caso de falha (rede ainda fora, ou o servidor recusou de novo); resolve normalmente em caso de sucesso. */
  readonly send: (action: string, payload: TPayload, idempotencyKey: string, occurredAt: string) => Promise<void>;
}

interface StoredAction<TPayload> {
  id?: number;
  action: string;
  payload: TPayload;
  idempotencyKey: string;
  occurredAt: string;
  attempts: number;
}

class OfflineActionQueueDb<TPayload> extends Dexie {
  actions!: Table<StoredAction<TPayload>, number>;

  constructor(dbName: string) {
    super(dbName);
    this.version(1).stores({ actions: '++id, occurredAt' });
  }
}

/**
 * Implementação real, com Dexie — usada em produção pelos módulos de fila de cada app
 * (`apps/web-pos/src/offline/pos-order-queue.ts`, `apps/web-menu/src/offline/menu-order-queue.ts`).
 */
export class ActionQueue<TPayload = unknown> implements OfflineActionQueue<TPayload> {
  private readonly db: OfflineActionQueueDb<TPayload>;
  // Evita reenvio concorrente — o evento `online` do navegador e uma reconexão de WebSocket podem
  // chegar quase juntos e chamar `flush()` duas vezes; sem essa trava, a segunda chamada leria a
  // mesma linha ainda não removida pela primeira e tentaria reenviar a MESMA ação em paralelo.
  private flushing = false;

  constructor(private readonly options: ActionQueueOptions<TPayload>) {
    this.db = new OfflineActionQueueDb<TPayload>(options.dbName);
  }

  async enqueue(
    action: string,
    payload: TPayload,
    idempotencyKey: string,
    occurredAt: string = new Date().toISOString(),
  ): Promise<void> {
    await this.db.actions.add({ action, payload, idempotencyKey, occurredAt, attempts: 0 });
  }

  async count(): Promise<number> {
    return this.db.actions.count();
  }

  async flush(): Promise<void> {
    if (this.flushing) return;
    this.flushing = true;
    try {
      // US-034 §7, cenário "queda momentânea da rede local": reordena por `occurredAt` — reenvia
      // na ordem em que as intenções nasceram, não na ordem em que a rede aceitou reenviar.
      const pending = await this.db.actions.orderBy('occurredAt').toArray();
      for (const item of pending) {
        const id = item.id;
        if (id === undefined) continue;
        try {
          await this.options.send(item.action, item.payload, item.idempotencyKey, item.occurredAt);
          await this.db.actions.delete(id);
        } catch {
          // Continua na fila para a próxima tentativa — a MESMA idempotencyKey (ADR-020) garante
          // que quando finalmente for aceita, não duplica, não importa quantas tentativas levou.
          // Cast explícito: `UpdateSpec<T>` calcula os caminhos válidos via um tipo condicional
          // recursivo (`KeyPaths`) que não resolve bem com `TPayload` genérico e não vinculado
          // aqui — `attempts` é, sem dúvida, uma chave de primeiro nível válida de `StoredAction`.
          await this.db.actions.update(id, { attempts: item.attempts + 1 } as UpdateSpec<StoredAction<TPayload>>);
        }
      }
    } finally {
      this.flushing = false;
    }
  }
}

/**
 * Distingue falha de REDE (o `fetch` nem chega a voltar uma `Response` — host inalcançável, DNS,
 * timeout de `AbortController`) de uma resposta HTTP de erro do servidor (essa já é tratada como
 * erro de negócio ANTES de chegar aqui, ver `requireSuccess` nos clientes de order-composition).
 * `fetch` rejeita com `TypeError` para falha de rede em todo navegador (ex.: "Failed to fetch",
 * "NetworkError when attempting to fetch resource"); `AbortError` é timeout por `AbortController`.
 * Só falha de rede deve enfileirar — um erro de validação real do servidor deve continuar
 * aparecendo pro operador, não silenciar numa fila que nunca vai ter sucesso.
 */
export function isNetworkFailure(error: unknown): boolean {
  return error instanceof TypeError || (error instanceof DOMException && error.name === 'AbortError');
}
