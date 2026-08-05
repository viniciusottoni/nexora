import {
  openSessionsResponseSchema,
  type OpenSessionsResponse,
  type OpenSessionsSortBy,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

export interface ListOpenSessionsOptions {
  /** Busca por mesa ou por comanda (US-050 §10) — substring, aplicada no servidor. */
  readonly search?: string;
  /** Padrão do servidor é "urgency" quando omitido — ver GetOpenSessionsQuery/CashierController. */
  readonly sortBy?: OpenSessionsSortBy;
}

/**
 * Cliente HTTP de `GET /v1/cash/open-sessions` (US-050 §7) — mesmo padrão de `TableMapApi`
 * (`table-map/table-map-api.ts`), mas para a tela do caixa (P4): só sessões abertas, sem
 * agrupamento por ambiente, com totalizador do salão.
 */
export class CashPanelApi {
  constructor(
    private readonly baseUrl = '',
    // Ver comentário equivalente em TableMapApi: `fetch` capturado bruto e chamado depois quebra
    // em navegador real ("Illegal invocation") — mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  async listOpenSessions(
    identity: Readonly<OperationalRequestIdentity>,
    options: ListOpenSessionsOptions = {},
  ): Promise<OpenSessionsResponse> {
    const params = new URLSearchParams();
    if (options.search) params.set('q', options.search);
    if (options.sortBy) params.set('sortBy', options.sortBy);
    const query = params.size > 0 ? `?${params.toString()}` : '';

    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/cash/open-sessions${query}`,
      { credentials: 'include' },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return openSessionsResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível carregar o painel do caixa.');
}
