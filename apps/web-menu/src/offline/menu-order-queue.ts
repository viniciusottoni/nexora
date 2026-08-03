import { ActionQueue, type OfflineActionQueue } from '@nexora/ui';
import { createPublicOrderRequestSchema, createOrderResponseSchema, type CreateOrderItemRequest } from '@nexora/contracts';

/**
 * Corpo de `POST /v1/public/orders` guardado na fila (US-034 §8) — o suficiente para reconstruir a
 * MESMA requisição no reenvio. Sem `sessionId`/`channel`: vêm das claims do `sessionToken`
 * (RN-015), por isso o token precisa ser guardado junto (ver `configureMenuOrderQueue`).
 */
export interface QueuedOrderPayload {
  readonly items: readonly CreateOrderItemRequest[];
}

interface QueueRuntime {
  sessionToken: string | undefined;
  baseUrl: string;
  fetcher: typeof fetch;
}

const runtime: QueueRuntime = {
  sessionToken: undefined,
  baseUrl: '',
  fetcher: (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
};

/**
 * Chamado pelo shell da tela de acesso do cliente (`BrandedTableMenu`, `table-access-page.tsx`)
 * assim que a sessão de mesa é resolvida — o `sessionToken` é o mesmo pelo tempo em que o cliente
 * fica na mesa, então basta configurar uma vez ao entrar.
 */
export function configureMenuOrderQueue(
  sessionToken: string,
  baseUrl = '',
  fetcher: typeof fetch = runtime.fetcher,
): void {
  runtime.sessionToken = sessionToken;
  runtime.baseUrl = baseUrl;
  runtime.fetcher = fetcher;
}

async function sendQueuedOrder(
  _action: string,
  payload: QueuedOrderPayload,
  idempotencyKey: string,
  occurredAt: string,
): Promise<void> {
  if (!runtime.sessionToken) {
    throw new Error('Sem sessão de mesa configurada para reenviar o pedido.');
  }
  const body = createPublicOrderRequestSchema.parse({ items: payload.items });
  const response = await runtime.fetcher(`${runtime.baseUrl}/v1/public/orders`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Authorization: `Bearer ${runtime.sessionToken}`,
      'Idempotency-Key': idempotencyKey,
      'X-Occurred-At': occurredAt,
    },
    body: JSON.stringify(body),
  });
  if (!response.ok) {
    throw new Error(`POST /v1/public/orders (reenvio da fila) recusado com status ${response.status}`);
  }
  createOrderResponseSchema.parse(await response.json());
}

/**
 * Fila ÚNICA do app (US-034 §8) — `dbName` fixo garante que toda `PublicOrderCompositionApi`
 * instanciada e o shell da mesa (`BrandedTableMenu`, que só precisa ler `count()`/chamar
 * `flush()`) leem e escrevem o MESMO IndexedDB.
 */
export const menuOrderQueue: OfflineActionQueue<QueuedOrderPayload> = new ActionQueue<QueuedOrderPayload>({
  dbName: 'nexora-web-menu-order-queue',
  send: sendQueuedOrder,
});
