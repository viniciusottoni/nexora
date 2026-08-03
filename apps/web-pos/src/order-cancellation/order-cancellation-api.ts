import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';
import {
  cancelOrderItemRequestSchema,
  cancelOrderItemResponseSchema,
  cancelOrderRequestSchema,
  cancelOrderResponseSchema,
  type CancelOrderItemResponse,
  type CancelOrderResponse,
} from '@nexora/contracts';

/** Erro de negócio com o código estável do ProblemDetails (ADR-021) — mesmo padrão de `OrderCompositionApiError`/`PosApiError`. */
export class OrderCancellationApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
    readonly meta?: Record<string, unknown>,
  ) {
    super(message);
    this.name = 'OrderCancellationApiError';
  }
}

/** Concessão de elevação pontual (ADR-023) — mesma forma de `AuthorizationGrant` de `operational-auth-client.ts`, devolvida crua aqui porque `/v1/auth/authorize` ainda não tem schema zod em `@nexora/contracts` (nenhum outro cliente desta solution valida essa resposta hoje). */
export interface OrderCancellationAuthorizationGrant {
  readonly authorizationToken: string;
  readonly expiresIn: number;
  readonly authorizedBy: Readonly<{ id: string; name: string }>;
}

/**
 * Cliente de cancelamento de item/pedido do garçom/POS (US-033 §7) — `PATCH
 * /v1/orders/{id}/items/{itemId}/cancel`, `POST /v1/orders/{id}/cancel` e `POST /v1/auth/authorize`
 * (ADR-023, elevação pontual). Mesmo padrão de `PosOrderCompositionApi` — identidade operacional
 * do dispositivo/PIN autentica todas as chamadas.
 */
export class PosOrderCancellationApi {
  constructor(
    private readonly identity: OperationalRequestIdentity,
    private readonly baseUrl = '',
    // Ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts sobre por que
    // `fetch` é resolvido a cada chamada (nunca capturado como valor) — mesmo padrão de
    // `PosOrderCompositionApi`.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  /**
   * `authorizationToken` é OPCIONAL — omitido na primeira tentativa; se o item já foi iniciado, o
   * servidor recusa com 403 `AUTHORIZATION_REQUIRED` (`meta: { action, itemStatus }`) e o chamador
   * repete a chamada com o token obtido de {@link authorize}.
   */
  async cancelItem(
    orderId: string,
    itemId: string,
    reason: string,
    notes: string | undefined,
    authorizationToken?: string,
  ): Promise<CancelOrderItemResponse> {
    const body = cancelOrderItemRequestSchema.parse({ reason, notes: notes ?? null });
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/orders/${orderId}/items/${itemId}/cancel`,
      {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
        },
        body: JSON.stringify(body),
      },
      this.identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return cancelOrderItemResponseSchema.parse(await response.json());
  }

  /** Mesma convenção de `authorizationToken` opcional de {@link cancelItem}. */
  async cancelOrder(
    orderId: string,
    reason: string,
    notes: string | undefined,
    authorizationToken?: string,
  ): Promise<CancelOrderResponse> {
    const body = cancelOrderRequestSchema.parse({ reason, notes: notes ?? null });
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/orders/${orderId}/cancel`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
        },
        body: JSON.stringify(body),
      },
      this.identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return cancelOrderResponseSchema.parse(await response.json());
  }

  /**
   * ADR-023, doc. 05 §2.2 — `POST /v1/auth/authorize { action, pin, context }`. `context` vincula
   * o token à ação E ao alvo específico (ex.: `{ orderItemId }`) — um token emitido para o item X
   * nunca autoriza o item Y.
   */
  async authorize(
    action: string,
    pin: string,
    context: Readonly<Record<string, unknown>>,
  ): Promise<OrderCancellationAuthorizationGrant> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/auth/authorize`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify({ action, pin, context }),
      },
      this.identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return (await response.json()) as OrderCancellationAuthorizationGrant;
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string; meta?: Record<string, unknown> }
    | null;
  throw new OrderCancellationApiError(
    problem?.detail ?? 'Não foi possível concluir a operação.',
    problem?.code,
    problem?.meta,
  );
}
