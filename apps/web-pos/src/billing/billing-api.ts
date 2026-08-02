import {
  assignBillItemsRequestSchema,
  billResponseSchema,
  partialPaymentResponseSchema,
  registerPartialPaymentRequestSchema,
  waiveServiceFeeRequestSchema,
  type AssignBillItemsRequest,
  type BillResponse,
  type PartialPaymentResponse,
  type RegisterPartialPaymentRequest,
  type WaiveServiceFeeRequest,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/** Erro de negócio com o código estável do ProblemDetails (ADR-021) — o chamador decide a mensagem. */
export class BillingApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
    readonly meta?: Record<string, unknown>,
  ) {
    super(message);
    this.name = 'BillingApiError';
  }
}

export interface GetBillOptions {
  readonly split?: 'BY_PERSON' | 'BY_ITEM' | 'BY_AMOUNT';
  readonly people?: number;
  readonly amount?: number;
  /** Pessoas que já optaram por não pagar a taxa de serviço (US-027 §4) — mantido no cliente, não persistido. */
  readonly waived?: readonly number[];
}

/**
 * Cliente de `GET /v1/sessions/{id}/bill` e das ações de divisão da conta no EDGE (US-027) — usado
 * pela tela de fechamento do caixa (`billing-page.tsx`). Mesmo padrão de autenticação operacional
 * de `PosTablesApi`/`TableMapApi`.
 */
export class BillingApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = fetch,
  ) {}

  async getBill(identity: Readonly<OperationalRequestIdentity>, sessionId: string, options: GetBillOptions = {}): Promise<BillResponse> {
    const params = new URLSearchParams();
    if (options.split) params.set('split', options.split);
    if (options.people !== undefined) params.set('people', String(options.people));
    if (options.amount !== undefined) params.set('amount', String(options.amount));
    if (options.waived && options.waived.length > 0) params.set('waived', options.waived.join(','));
    const query = params.size > 0 ? `?${params.toString()}` : '';

    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/bill${query}`,
      {},
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return billResponseSchema.parse(await response.json());
  }

  /**
   * Modo BY_ITEM (US-027 §7) — não persiste no servidor (o POS reenvia a atribuição completa a
   * cada chamada, ver docstring de `AssignBillItemsCommand` no backend).
   */
  async assignItems(identity: Readonly<OperationalRequestIdentity>, sessionId: string, input: AssignBillItemsRequest): Promise<BillResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/bill/assign-items`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(assignBillItemsRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return billResponseSchema.parse(await response.json());
  }

  /** US-027 §4, cenário "Retirada da taxa por uma das partes" — registrada e auditada (RN-010). */
  async waiveServiceFee(identity: Readonly<OperationalRequestIdentity>, sessionId: string, input: WaiveServiceFeeRequest): Promise<BillResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/bill/waive-service-fee`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(waiveServiceFeeRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return billResponseSchema.parse(await response.json());
  }

  /** US-027 §4, cenário "Divisão por valor" — a sessão permanece em BILL_REQUESTED depois de registrado. */
  async registerPartialPayment(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    input: RegisterPartialPaymentRequest,
  ): Promise<PartialPaymentResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/bill/partial-payment`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(registerPartialPaymentRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return partialPaymentResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string; meta?: Record<string, unknown> }
    | null;
  throw new BillingApiError(
    problem?.detail ?? 'Não foi possível concluir a operação.',
    problem?.code,
    problem?.meta,
  );
}
