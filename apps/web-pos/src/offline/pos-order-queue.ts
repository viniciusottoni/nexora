import {
  ActionQueue,
  operationalAuthenticatedFetch,
  type OfflineActionQueue,
  type OperationalRequestIdentity,
} from '@nexora/ui';
import { createOrderRequestSchema, createOrderResponseSchema, type CreateOrderItemRequest } from '@nexora/contracts';

/**
 * Corpo de `POST /v1/orders` guardado na fila (US-034 §8) — o suficiente para reconstruir a MESMA
 * requisição no reenvio; `channel` não entra aqui porque, no garçom, é sempre `'DineIn'`
 * (ver docstring de `PosOrderCompositionApi.createOrder`).
 */
export interface QueuedOrderPayload {
  readonly sessionId: string | null;
  readonly items: readonly CreateOrderItemRequest[];
}

interface QueueRuntime {
  identity: OperationalRequestIdentity | undefined;
  baseUrl: string;
  fetcher: typeof fetch;
}

const runtime: QueueRuntime = {
  identity: undefined,
  baseUrl: '',
  fetcher: (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
};

/**
 * Chamado pelo shell autenticado (`BrandedPos`, `app.tsx`) assim que a identidade operacional do
 * dispositivo é conhecida — é o que permite ao reenvio automático (`flush`) usar a identidade
 * CORRENTE, não a de quando o pedido entrou na fila (o `accessToken` pode ter expirado e sido
 * renovado nesse meio-tempo; `deviceId`/`deviceSecret` são estáveis pelo dispositivo).
 */
export function configurePosOrderQueue(
  identity: OperationalRequestIdentity,
  baseUrl = '',
  fetcher: typeof fetch = runtime.fetcher,
): void {
  runtime.identity = identity;
  runtime.baseUrl = baseUrl;
  runtime.fetcher = fetcher;
}

async function sendQueuedOrder(
  _action: string,
  payload: QueuedOrderPayload,
  idempotencyKey: string,
  occurredAt: string,
): Promise<void> {
  if (!runtime.identity) {
    // Ainda sem identidade configurada (ex.: dispositivo acabou de reiniciar e nem autenticou de
    // novo) — não é falha de rede, mas o efeito prático precisa ser o mesmo: continua na fila e
    // tenta de novo no próximo reconecte/evento `online`, sem derrubar a Promise sem motivo.
    throw new Error('Sem identidade operacional configurada para reenviar o pedido.');
  }
  const body = createOrderRequestSchema.parse({ channel: 'DineIn', sessionId: payload.sessionId, items: payload.items });
  const response = await operationalAuthenticatedFetch(
    `${runtime.baseUrl}/v1/orders`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': idempotencyKey, 'X-Occurred-At': occurredAt },
      body: JSON.stringify(body),
    },
    runtime.identity,
    runtime.fetcher,
  );
  if (!response.ok) {
    throw new Error(`POST /v1/orders (reenvio da fila) recusado com status ${response.status}`);
  }
  createOrderResponseSchema.parse(await response.json());
}

/**
 * Fila ÚNICA do app (US-034 §8) — `dbName` fixo garante que toda `PosOrderCompositionApi`
 * instanciada (uma por sessão, `useMemo` em `OrderCompositionPage`) e o shell autenticado
 * (`BrandedPos`, que só precisa ler `count()`/chamar `flush()`) leem e escrevem o MESMO IndexedDB
 * — o pedido enfileirado numa tela sobrevive à navegação para outra.
 */
export const posOrderQueue: OfflineActionQueue<QueuedOrderPayload> = new ActionQueue<QueuedOrderPayload>({
  dbName: 'nexora-web-pos-order-queue',
  send: sendQueuedOrder,
});
