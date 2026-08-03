import {
  openTableSessionRequestSchema,
  tableMapResponseSchema,
  tableSessionSchema,
  type OpenTableSessionRequest,
  type TableMapEntry,
  type TableSessionDto,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/** Erro de negócio com o código estável do ProblemDetails (ADR-021) — o chamador decide a mensagem. */
export class PosApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
    readonly meta?: Record<string, unknown>,
  ) {
    super(message);
    this.name = 'PosApiError';
  }
}

/**
 * Cliente de `GET /v1/tables` e `POST /v1/tables/{id}/sessions` no EDGE (US-022) — usado pelo
 * fluxo "abrir mesa" do garçom. Diferente de `apps/web-admin/src/tables/tables-api.ts` (que fala
 * com a nuvem via cookie de sessão administrativa), este cliente autentica com a identidade
 * operacional do dispositivo/PIN (`operationalAuthenticatedFetch`), a mesma usada pelo restante do
 * `web-pos`.
 */
export class PosTablesApi {
  constructor(
    private readonly identity: OperationalRequestIdentity,
    private readonly baseUrl = '',
    // (...args: Parameters<typeof fetch>) => globalThis.fetch(...args): ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts
    // — `fetch` capturado bruto e chamado depois como `this.fetcher(...)` quebra em navegador real
    // ("Illegal invocation"), mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  /**
   * Mesas livres — filtradas no cliente a partir do mapa completo de mesas (US-023,
   * `GET /v1/tables`), que substituiu a listagem mínima desta história (US-022) quando o mapa
   * ficou pronto. Suficiente para o fluxo de abertura: escolher a mesa e ver a capacidade dela.
   */
  async listFreeTables(): Promise<TableMapEntry[]> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/tables`,
      {},
      this.identity,
      this.fetcher,
    );
    await requireSuccess(response);
    const all = tableMapResponseSchema.parse(await response.json());
    return all.tables.filter((table) => table.status === 'FREE');
  }

  /**
   * Cenário Gherkin "Abertura pelo garçom" (US-022 §4). `Idempotency-Key` nova a cada intenção de
   * abertura (gerada aqui, uma vez por toque em "abrir mesa" — não a cada tentativa de rede, ADR-020).
   * `X-Occurred-At` preserva o horário real da abertura mesmo com sync atrasado (RN-020).
   */
  async openSession(tableId: string, input: OpenTableSessionRequest): Promise<TableSessionDto> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/tables/${encodeURIComponent(tableId)}/sessions`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          'X-Occurred-At': new Date().toISOString(),
        },
        body: JSON.stringify(openTableSessionRequestSchema.parse(input)),
      },
      this.identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return tableSessionSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string; meta?: Record<string, unknown> }
    | null;
  throw new PosApiError(
    problem?.detail ?? 'Não foi possível concluir a operação.',
    problem?.code,
    problem?.meta,
  );
}
