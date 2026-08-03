import {
  acknowledgeWaiterCallResponseSchema,
  requestBillResponseSchema,
  tableMapResponseSchema,
  type AcknowledgeWaiterCallResponse,
  type BillSplitMode,
  type RequestBillResponse,
  type TableMapResponse,
  type TableMapSortBy,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

export interface ListTableMapOptions {
  /** "Minhas mesas" (US-023 §4, filtro por responsabilidade). */
  readonly mine?: boolean;
  /** Padrão do servidor é "urgency" quando omitido — ver GetTableMapQuery/TablesController. */
  readonly sortBy?: TableMapSortBy;
}

/**
 * Cliente HTTP de `GET /v1/tables` (US-023 §7) — mesmo padrão de `apps/web-admin/src/devices/devices-api.ts`.
 * US-025/US-026: também expõe as duas ações de escrita que partem do mapa (garçom confirma
 * atendimento, garçom marca "conta solicitada") — vivem aqui, não num arquivo `waiter-alerts-api.ts`
 * separado, porque as duas só fazem sentido a partir de uma mesa já carregada pelo mapa.
 */
export class TableMapApi {
  constructor(
    private readonly baseUrl = '',
    // (...args: Parameters<typeof fetch>) => globalThis.fetch(...args): ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts
    // — `fetch` capturado bruto e chamado depois como `this.fetcher(...)` quebra em navegador real
    // ("Illegal invocation"), mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  async list(identity: Readonly<OperationalRequestIdentity>, options: ListTableMapOptions = {}): Promise<TableMapResponse> {
    const params = new URLSearchParams();
    if (options.mine) params.set('mine', 'true');
    if (options.sortBy) params.set('sortBy', options.sortBy);
    const query = params.size > 0 ? `?${params.toString()}` : '';

    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/tables${query}`,
      { credentials: 'include' },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return tableMapResponseSchema.parse(await response.json());
  }

  /** US-025 §7 — o garçom confirma que atendeu a chamada da mesa (resolve o alerta pendente). */
  async acknowledgeWaiterCall(
    identity: Readonly<OperationalRequestIdentity>,
    tableId: string,
  ): Promise<AcknowledgeWaiterCallResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/tables/${encodeURIComponent(tableId)}/acknowledge-call`,
      { method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() } },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return acknowledgeWaiterCallResponseSchema.parse(await response.json());
  }

  /** US-026 §7, cenário "Solicitação pelo garçom" — mesmo efeito de pedir a conta pelo cardápio. */
  async requestBill(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    splitMode: BillSplitMode,
    people?: number,
  ): Promise<RequestBillResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/request-bill`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(people !== undefined ? { splitMode, people } : { splitMode }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return requestBillResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
