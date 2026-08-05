import {
  applyDiscountRequestSchema,
  applyDiscountResponseSchema,
  assignBillItemsRequestSchema,
  billResponseSchema,
  getReceiptResponseSchema,
  partialPaymentResponseSchema,
  printReceiptResponseSchema,
  registerPartialPaymentRequestSchema,
  registerPaymentsRequestSchema,
  registerPaymentsResponseSchema,
  waiveServiceFeeRequestSchema,
  waiveSessionServiceFeeRequestSchema,
  waiveSessionServiceFeeResponseSchema,
  type ApplyDiscountRequest,
  type ApplyDiscountResponse,
  type AssignBillItemsRequest,
  type BillResponse,
  type GetReceiptResponse,
  type PartialPaymentResponse,
  type PrintReceiptResponse,
  type RegisterPartialPaymentRequest,
  type RegisterPaymentsRequest,
  type RegisterPaymentsResponse,
  type WaiveServiceFeeRequest,
  type WaiveSessionServiceFeeRequest,
  type WaiveSessionServiceFeeResponse,
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

  /**
   * US-027 §4, cenário "Divisão por valor" — a sessão permanece em BILL_REQUESTED depois de
   * registrado. <paramref name="authorizationToken"/> (US-035 §10) só é enviado quando o caixa
   * autorizou o fechamento com item pendente (modo BLOCK) — token emitido por
   * `authorizeCloseWithPending`, vinculado à ação `CLOSE_WITH_PENDING` (ADR-023).
   */
  async registerPartialPayment(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    input: RegisterPartialPaymentRequest,
    authorizationToken?: string,
  ): Promise<PartialPaymentResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/bill/partial-payment`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
        },
        body: JSON.stringify(registerPartialPaymentRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return partialPaymentResponseSchema.parse(await response.json());
  }

  /**
   * US-035 §10 ("Autorização no mesmo dispositivo, sem trocar de sessão") — elevação pontual
   * (ADR-023) para a ação `CLOSE_WITH_PENDING`: o gerente informa o próprio PIN no MESMO terminal
   * em que o caixa está, sem precisar trocar de sessão. O token devolvido é enviado como
   * `X-Authorization-Token` na próxima chamada a `registerPartialPayment`.
   */
  async authorizeCloseWithPending(
    identity: Readonly<OperationalRequestIdentity>,
    input: { readonly sessionId: string; readonly pin: string; readonly reason?: string | undefined },
  ): Promise<AuthorizeCloseWithPendingResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/auth/authorize`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          action: 'CLOSE_WITH_PENDING',
          pin: input.pin,
          context: { sessionId: input.sessionId, reason: input.reason ?? null },
        }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return response.json() as Promise<AuthorizeCloseWithPendingResponse>;
  }

  /**
   * US-054 §4 ("o gerente digita o PIN no próprio dispositivo do operador, sem trocar de sessão")
   * — mesma elevação pontual (ADR-023) de `authorizeCloseWithPending`, ação `DISCOUNT_ABOVE_LIMIT`.
   */
  async authorizeDiscount(
    identity: Readonly<OperationalRequestIdentity>,
    input: { readonly sessionId: string; readonly pin: string; readonly reason?: string | undefined },
  ): Promise<AuthorizeCloseWithPendingResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/auth/authorize`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          action: 'DISCOUNT_ABOVE_LIMIT',
          pin: input.pin,
          context: { sessionId: input.sessionId, reason: input.reason ?? null },
        }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return response.json() as Promise<AuthorizeCloseWithPendingResponse>;
  }

  /**
   * US-052 (Múltiplas formas de pagamento) / US-058 (Pagamento de maquininha externa) —
   * confirmação ÚNICA do conjunto de pagamentos (US-052 §10), fecha a comanda por completo
   * (diferente de `registerPartialPayment`, que é US-027 e mantém a sessão em aberto).
   */
  async registerPayments(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    input: RegisterPaymentsRequest,
  ): Promise<RegisterPaymentsResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/payments`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(registerPaymentsRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return registerPaymentsResponseSchema.parse(await response.json());
  }

  /** US-054 (Desconto com autorização) — acima do limite exige `authorizationToken` (ADR-023, ação DISCOUNT_ABOVE_LIMIT). */
  async applyDiscount(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    input: ApplyDiscountRequest,
    authorizationToken?: string,
  ): Promise<ApplyDiscountResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/discount`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
        },
        body: JSON.stringify(applyDiscountRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return applyDiscountResponseSchema.parse(await response.json());
  }

  /**
   * US-053 (Taxa de serviço com retirada registrada) — registro AUTORITATIVO no nível da sessão
   * (RN-010), distinto de `waiveServiceFee` (US-027, prévia efêmera por pessoa).
   */
  async waiveSessionServiceFee(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    input: WaiveSessionServiceFeeRequest,
  ): Promise<WaiveSessionServiceFeeResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/service-fee/waive`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(waiveSessionServiceFeeRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return waiveSessionServiceFeeResponseSchema.parse(await response.json());
  }

  /** US-057 (Comprovante não fiscal) — só existe depois que a conta é paga. */
  async getReceipt(identity: Readonly<OperationalRequestIdentity>, sessionId: string): Promise<GetReceiptResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/receipt`,
      {},
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return getReceiptResponseSchema.parse(await response.json());
  }

  /** US-057 §4, cenário "Impressora indisponível" — nunca lança por falha de hardware, só por sessão inexistente. */
  async printReceipt(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    printerId?: string,
  ): Promise<PrintReceiptResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/receipt/print`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ printerId: printerId ?? null }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return printReceiptResponseSchema.parse(await response.json());
  }

  /** US-057 §4, cenário "Reimpressão auditada" — registrada em `audit_log` com autor e horário. */
  async reprintReceipt(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    printerId?: string,
  ): Promise<PrintReceiptResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/sessions/${encodeURIComponent(sessionId)}/receipt/reprint`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ printerId: printerId ?? null }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return printReceiptResponseSchema.parse(await response.json());
  }
}

/** Porta de <c>AuthorizeSensitiveActionResponse</c> (backend) — só o suficiente para esta tela. */
export interface AuthorizeCloseWithPendingResponse {
  readonly authorizationToken: string;
  readonly expiresIn: number;
  readonly authorizedBy: { readonly id: string; readonly name: string };
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
