import {
  callWaiterResponseSchema,
  requestBillResponseSchema,
  type BillSplitMode,
  type CallWaiterResponse,
  type RequestBillResponse,
} from '@nexora/contracts';

/** Erro de negócio de chamar garçom/pedir a conta — carrega o código estável do ProblemDetails (ADR-021). */
export class WaiterCallApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
  ) {
    super(message);
    this.name = 'WaiterCallApiError';
  }
}

/**
 * Cliente de `POST /v1/public/table/{qrToken}/call-waiter` e `POST /v1/public/table/{qrToken}/request-bill`
 * (US-025/US-026), autenticado com o mesmo `sessionToken` anônimo do esquema `TableSession` usado
 * por `ConsumptionApi` (ver `consumption-api.ts`). `qrToken` viaja na rota por simetria com o
 * contrato de API da história — a autoridade de fato é sempre o token (RN-015): o backend confere
 * que o `qrToken` ainda resolve para a MESMA mesa/sessão do token antes de agir.
 */
export class WaiterCallApi {
  constructor(
    private readonly qrToken: string,
    private readonly sessionToken: string,
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = fetch,
  ) {}

  /**
   * US-025 §7 — `Idempotency-Key` obrigatório (ADR-020), gerado uma vez por toque. Nunca lança
   * para "chamada repetida": o backend devolve 202 com `alreadyPending: true`, não um erro — o
   * chamador lê o campo, não uma exceção, para mostrar "o garçom já foi avisado".
   */
  async callWaiter(): Promise<CallWaiterResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/public/table/${encodeURIComponent(this.qrToken)}/call-waiter`,
      {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${this.sessionToken}`,
          'Idempotency-Key': cryptoRandomUuid(),
        },
      },
    );
    await requireSuccess(response);
    return callWaiterResponseSchema.parse(await response.json());
  }

  /** US-026 §7 — `people` só é enviado quando faz sentido (`BY_PERSON`); os demais modos não exigem contagem. */
  async requestBill(splitMode: BillSplitMode, people?: number): Promise<RequestBillResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/public/table/${encodeURIComponent(this.qrToken)}/request-bill`,
      {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          Authorization: `Bearer ${this.sessionToken}`,
          'Idempotency-Key': cryptoRandomUuid(),
        },
        body: JSON.stringify(people !== undefined ? { splitMode, people } : { splitMode }),
      },
    );
    await requireSuccess(response);
    return requestBillResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string; code?: string } | null;
  throw new WaiterCallApiError(problem?.detail ?? 'Não foi possível concluir a operação.', problem?.code);
}

function cryptoRandomUuid(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  // Fallback só para ambiente de teste sem `crypto.randomUUID` (jsdom antigo) — nunca em produção.
  return `xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
