import {
  composableMenuResponseSchema,
  isNetworkFailure,
  mapComposableMenuResponseToProducts,
  type ComposableProduct,
  type OfflineActionQueue,
} from '@nexora/ui';
import {
  createOrderResponseSchema,
  createPublicOrderRequestSchema,
  type CreateOrderItemRequest,
  type CreateOrderResponse,
} from '@nexora/contracts';
import { menuOrderQueue, type QueuedOrderPayload } from '../offline/menu-order-queue.js';

/** Erro de negócio do pedido do cliente — carrega o código estável do ProblemDetails (ADR-021), mesmo padrão de `PublicTableApiError`/`WaiterCallApiError`. */
export class OrderCompositionApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
    readonly meta?: Record<string, unknown>,
  ) {
    super(message);
    this.name = 'OrderCompositionApiError';
  }
}

/**
 * Resultado de `createOrder` (US-034 §7/§10) — `'sent'` é o caminho de sempre (US-030), `'queued'`
 * é o envio otimista quando a rede local caiu: nenhuma `Response` do servidor ainda existe, então
 * a tela não tem o `shortCode` real ainda — só a garantia de que a `idempotencyKey` já foi fixada
 * e vai ser reenviada sem duplicar (ADR-020) assim que a conexão voltar.
 */
export type CreateOrderOutcome =
  | { readonly status: 'sent'; readonly order: CreateOrderResponse['order']; readonly promisedAt: string; readonly estimatedMinutes: number }
  | { readonly status: 'queued'; readonly idempotencyKey: string };

/**
 * Cliente de composição de pedido do cliente pela mesa (US-030 §7, caminho `POST /v1/public/orders`)
 * — `GET /v1/public/menu` (anônimo, mesmo endpoint já usado por `PublicTableApi.getMenu()`; ver
 * docstring de `@nexora/ui`'s `composable-menu.ts` sobre o que ele NÃO traz hoje) e o envio do
 * pedido autenticado pelo `sessionToken` anônimo da mesa (mesmo padrão de `WaiterCallApi`).
 */
export class PublicOrderCompositionApi {
  constructor(
    private readonly sessionToken: string,
    private readonly baseUrl = '',
    // (...args: Parameters<typeof fetch>) => globalThis.fetch(...args): ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts
    // — `fetch` capturado bruto e chamado depois como `this.fetcher(...)` quebra em navegador real
    // ("Illegal invocation"), mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
    // Injetável (mesmo padrão de `TableMapHubConnection`/`KdsHubConnection`) — produção usa a fila
    // ÚNICA do app (`menuOrderQueue`, dbName fixo); teste injeta um duplo, sem abrir IndexedDB.
    private readonly queue: OfflineActionQueue<QueuedOrderPayload> = menuOrderQueue,
  ) {}

  async getMenu(): Promise<ComposableProduct[]> {
    const response = await this.fetcher(`${this.baseUrl}/v1/public/menu?channel=DineIn`, {
      headers: { Accept: 'application/json' },
    });
    await requireSuccess(response);
    const menu = composableMenuResponseSchema.parse(await response.json());
    return mapComposableMenuResponseToProducts(menu);
  }

  /**
   * US-030 §7, cenário "Pedido do cliente na mesa" — `channel`/`sessionId` NÃO vão no corpo (vêm
   * das claims do próprio `sessionToken`, RN-015). `Idempotency-Key` gerada UMA vez por toque em
   * "Confirmar pedido" (ADR-020) e preservada entre tentativas — se o `fetch` falhar por REDE
   * (US-034 §7, cenário "queda momentânea da rede local"/"cliente montando o pedido quando a
   * conexão cai"), a MESMA chave vai para a fila local do dispositivo em vez de virar um erro na
   * tela do cliente; se for uma resposta HTTP de erro de negócio, segue o caminho de sempre
   * (`requireSuccess`).
   */
  async createOrder(items: readonly CreateOrderItemRequest[]): Promise<CreateOrderOutcome> {
    const idempotencyKey = crypto.randomUUID();
    const occurredAt = new Date().toISOString();
    const body = createPublicOrderRequestSchema.parse({ items });

    let response: Response;
    try {
      response = await this.fetcher(`${this.baseUrl}/v1/public/orders`, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          Authorization: `Bearer ${this.sessionToken}`,
          'Idempotency-Key': idempotencyKey,
          'X-Occurred-At': occurredAt,
        },
        body: JSON.stringify(body),
      });
    } catch (cause) {
      if (!isNetworkFailure(cause)) throw cause;
      // Envio otimista (US-034 §4, cenário "queda no meio de um pedido": "o cliente não deve
      // perceber diferença no fluxo") — a ação entra na fila local com a chave já fixada.
      const payload: QueuedOrderPayload = { items };
      await this.queue.enqueue('order.create', payload, idempotencyKey, occurredAt);
      return { status: 'queued', idempotencyKey };
    }
    await requireSuccess(response);
    const parsed = createOrderResponseSchema.parse(await response.json());
    return { status: 'sent', order: parsed.order, promisedAt: parsed.promisedAt, estimatedMinutes: parsed.estimatedMinutes };
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string; meta?: Record<string, unknown> }
    | null;
  throw new OrderCompositionApiError(
    problem?.detail ?? 'Não foi possível concluir a operação.',
    problem?.code,
    problem?.meta,
  );
}
