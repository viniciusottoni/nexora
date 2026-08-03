import { getKdsQueueResponseSchema, type GetKdsQueueResponse } from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/**
 * Cliente HTTP de `GET /v1/kds/queue` (US-031 §7) — fallback de polling do ADR-011 (a cada 5 s no
 * cliente) e também a primeira carga da tela (antes de qualquer evento chegar pelo WebSocket).
 * Mesmo padrão de `apps/web-pos/src/table-map/table-map-api.ts`.
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
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível consultar a fila da cozinha agora.');
}
