import {
  advanceKdsOrderResponseSchema,
  getKdsQueueResponseSchema,
  kdsOrderItemResponseSchema,
  type AdvanceKdsOrderResponse,
  type GetKdsQueueResponse,
  type KdsOrderItemResponse,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/** Código de erro devolvido quando o código curto digitado no teclado não corresponde a nenhum pedido ativo nesta praça (US-041 §7). */
export const KDS_SHORT_CODE_NOT_FOUND = 'SHORT_CODE_NOT_FOUND';

/** Erro de API com o `code` estável do RFC 7807 (ADR-021) preservado — o teclado precisa distingui-lo de qualquer outra falha para limpar o campo sem travar a tela (US-041 §4). */
export class KdsApiError extends Error {
  constructor(
    message: string,
    readonly code: string | undefined,
  ) {
    super(message);
    this.name = 'KdsApiError';
  }
}

/**
 * Cliente HTTP da fila e do avanço/desfazer do KDS (US-031/US-040/US-041) — `queue` é também o
 * fallback de polling do ADR-011 (a cada 5 s no cliente). Mesmo padrão de
 * `apps/web-pos/src/table-map/table-map-api.ts`.
 */
export class KdsQueueApi {
  constructor(
    private readonly baseUrl = '',
    // Ver comentário em table-map-api.ts sobre por que não é `fetch.bind(globalThis)`.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  async queue(
    identity: Readonly<OperationalRequestIdentity>,
    stationId: string,
    since?: string,
  ): Promise<GetKdsQueueResponse> {
    const params = new URLSearchParams({ stationId });
    if (since) params.set('since', since);

    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/kds/queue?${params.toString()}`,
      { credentials: 'include' },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return getKdsQueueResponseSchema.parse(await response.json());
  }

  /**
   * US-041 §7 — caminho principal do teclado numérico: código curto do pedido + Enter.
   * `batch=false` (padrão) avança só o item mais antigo ainda ativo do pedido nesta praça;
   * `batch=true` é a confirmação explícita de avanço em lote (ver docstring de
   * `AdvanceKdsOrderCommand` no backend para a decisão completa).
   */
  async advanceOrder(
    identity: Readonly<OperationalRequestIdentity>,
    shortCode: string,
    stationId: string,
    batch = false,
  ): Promise<AdvanceKdsOrderResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/kds/orders/${encodeURIComponent(shortCode)}/advance`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify({ stationId, batch }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return advanceKdsOrderResponseSchema.parse(await response.json());
  }

  /** US-041 §7 — avanço direto por toque no cartão (não pelo teclado), pelo id do próprio item. */
  async advanceItem(
    identity: Readonly<OperationalRequestIdentity>,
    itemId: string,
  ): Promise<KdsOrderItemResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/kds/items/${encodeURIComponent(itemId)}/advance`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: '{}',
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return kdsOrderItemResponseSchema.parse(await response.json());
  }

  /** US-041 §3/§4 — desfazer o último avanço, janela de 10 s no servidor. */
  async undoItem(
    identity: Readonly<OperationalRequestIdentity>,
    itemId: string,
  ): Promise<KdsOrderItemResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/kds/items/${encodeURIComponent(itemId)}/undo`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: '{}',
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return kdsOrderItemResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string }
    | null;
  throw new KdsApiError(
    problem?.detail ?? 'Não foi possível concluir a operação na fila da cozinha.',
    problem?.code,
  );
}
