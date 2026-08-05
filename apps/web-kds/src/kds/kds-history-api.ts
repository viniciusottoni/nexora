import { getKdsHistoryResponseSchema, type GetKdsHistoryResponse } from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/** Erro de API com o `code` estável do RFC 7807 (ADR-021) preservado — mesmo padrão de `KdsApiError` em `kds-queue-api.ts`. */
export class KdsHistoryApiError extends Error {
  constructor(
    message: string,
    readonly code: string | undefined,
  ) {
    super(message);
    this.name = 'KdsHistoryApiError';
  }
}

/**
 * Cliente HTTP do histórico do turno (US-046) — lê `GET /v1/kds/history`, itens SERVIDOS da praça
 * dentro do dia operacional corrente (ADR-018), com busca opcional por código curto do pedido ou
 * mesa. Mesmo padrão de `KdsQueueApi` (`kds-queue-api.ts`).
 */
export class KdsHistoryApi {
  constructor(
    private readonly baseUrl = '',
    // Ver comentário em kds-queue-api.ts sobre por que não é `fetch.bind(globalThis)`.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  async history(
    identity: Readonly<OperationalRequestIdentity>,
    stationId: string,
    search?: string,
  ): Promise<GetKdsHistoryResponse> {
    const params = new URLSearchParams({ shift: 'current', stationId });
    if (search) params.set('search', search);

    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/kds/history?${params.toString()}`,
      { credentials: 'include' },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return getKdsHistoryResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string }
    | null;
  throw new KdsHistoryApiError(
    problem?.detail ?? 'Não foi possível carregar o histórico do turno.',
    problem?.code,
  );
}
