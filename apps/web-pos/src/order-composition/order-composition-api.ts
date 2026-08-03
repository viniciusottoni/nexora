import {
  composableMenuResponseSchema,
  isNetworkFailure,
  mapComposableMenuResponseToProducts,
  operationalAuthenticatedFetch,
  type ComposableProduct,
  type OfflineActionQueue,
  type OperationalRequestIdentity,
} from '@nexora/ui';
import {
  createOrderRequestSchema,
  createOrderResponseSchema,
  type CreateOrderItemRequest,
  type CreateOrderResponse,
} from '@nexora/contracts';
import { posOrderQueue, type QueuedOrderPayload } from '../offline/pos-order-queue.js';

/** Erro de negócio com o código estável do ProblemDetails (ADR-021) — mesmo padrão de `PosApiError` (`tables-api.ts`). */
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
 * é o envio otimista quando a rede local caiu: nenhuma `Response` do servidor ainda existe (o
 * pedido só será confirmado de verdade quando a fila reenviar com sucesso), então a tela NÃO tem o
 * `shortCode`/`order.id` reais — só a garantia de que a `idempotencyKey` já foi fixada e vai ser
 * reenviada, sem duplicar (ADR-020), assim que a conexão voltar.
 */
export type CreateOrderOutcome =
  | { readonly status: 'sent'; readonly order: CreateOrderResponse['order']; readonly promisedAt: string; readonly estimatedMinutes: number }
  | { readonly status: 'queued'; readonly idempotencyKey: string };

/**
 * Cliente de composição de pedido do garçom (US-030) — `GET /v1/public/menu` (anônimo, mesmo
 * endpoint do cardápio do cliente — ver docstring de `composable-menu.ts` sobre o que ele NÃO
 * traz hoje) e `POST /v1/orders` (autenticado pela identidade operacional do dispositivo/PIN,
 * mesmo padrão de `PosTablesApi`/`TableMapApi`).
 */
export class PosOrderCompositionApi {
  constructor(
    private readonly identity: OperationalRequestIdentity,
    private readonly baseUrl = '',
    // (...args: Parameters<typeof fetch>) => globalThis.fetch(...args): ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts
    // — `fetch` capturado bruto e chamado depois como `this.fetcher(...)` quebra em navegador real
    // ("Illegal invocation"), mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
    // Injetável (mesmo padrão de `TableMapHubConnection`/`KdsHubConnection`) — produção usa a fila
    // ÚNICA do app (`posOrderQueue`, dbName fixo); teste injeta um duplo, sem abrir IndexedDB.
    private readonly queue: OfflineActionQueue<QueuedOrderPayload> = posOrderQueue,
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
   * US-030 §7, cenário "Pedido pelo celular do garçom" — `channel` sempre `DineIn` (o garçom lança
   * pela mesa; balcão/delivery são fora do escopo desta tela). `Idempotency-Key` gerada UMA vez por
   * toque em "Confirmar pedido" (ADR-020) e preservada entre tentativas — se o `fetch` falhar por
   * REDE (US-034 §7, "queda momentânea da rede local"), a MESMA chave vai para a fila local em vez
   * de ser descartada; se for uma resposta HTTP de erro de negócio, segue o caminho de sempre
   * (`requireSuccess`, US-030). `X-Occurred-At` preserva o horário real mesmo com sync atrasado
   * (RN-020) — vale tanto para o envio direto quanto para o reenvio pela fila.
   */
  async createOrder(sessionId: string, items: readonly CreateOrderItemRequest[]): Promise<CreateOrderOutcome> {
    const idempotencyKey = crypto.randomUUID();
    const occurredAt = new Date().toISOString();
    const body = createOrderRequestSchema.parse({ channel: 'DineIn', sessionId, items });

    let response: Response;
    try {
      response = await operationalAuthenticatedFetch(
        `${this.baseUrl}/v1/orders`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Idempotency-Key': idempotencyKey,
            'X-Occurred-At': occurredAt,
          },
          body: JSON.stringify(body),
        },
        this.identity,
        this.fetcher,
      );
    } catch (cause) {
      if (!isNetworkFailure(cause)) throw cause;
      // Envio otimista (US-034 §10): nunca propaga erro pro operador por uma queda de LAN — a
      // ação entra na fila local do dispositivo com a chave já fixada, e sai de lá sem duplicar
      // assim que a conexão voltar (ADR-020).
      const payload: QueuedOrderPayload = { sessionId, items };
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
