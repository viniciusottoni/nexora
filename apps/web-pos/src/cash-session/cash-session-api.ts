import {
  closeCashSessionRequestSchema,
  closeCashSessionResponseSchema,
  getCurrentCashSessionResponseSchema,
  listCashMovementsResponseSchema,
  openCashSessionRequestSchema,
  openCashSessionResponseSchema,
  registerCashMovementRequestSchema,
  registerCashMovementResponseSchema,
  type CloseCashSessionRequest,
  type CloseCashSessionResponse,
  type GetCurrentCashSessionResponse,
  type ListCashMovementsResponse,
  type OpenCashSessionRequest,
  type OpenCashSessionResponse,
  type RegisterCashMovementRequest,
  type RegisterCashMovementResponse,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/** Erro de negócio com o código estável do ProblemDetails (ADR-021) — o chamador decide a mensagem. */
export class CashSessionApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
    readonly meta?: Record<string, unknown>,
  ) {
    super(message);
    this.name = 'CashSessionApiError';
  }
}

/** Porta de `AuthorizeSensitiveActionResponse` (backend) — só o suficiente para esta tela. */
export interface AuthorizeCashActionResponse {
  readonly authorizationToken: string;
  readonly expiresIn: number;
  readonly authorizedBy: { readonly id: string; readonly name: string };
}

/**
 * Cliente de `/v1/cash-sessions/*` no EDGE (US-055 abertura/fechamento, US-056 sangria/suprimento)
 * — mesmo padrão de autenticação operacional de `BillingApi`/`PosTablesApi`.
 */
export class CashSessionApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = fetch,
  ) {}

  /** Cenário Gherkin "Abertura com fundo" (US-055 §4). */
  async open(identity: Readonly<OperationalRequestIdentity>, input: OpenCashSessionRequest): Promise<OpenCashSessionResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/cash-sessions/open`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(openCashSessionRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return openCashSessionResponseSchema.parse(await response.json());
  }

  /** Sessão aberta/em conferência do operador corrente, com a composição do valor esperado (US-055 §7/§10). */
  async getCurrent(identity: Readonly<OperationalRequestIdentity>): Promise<GetCurrentCashSessionResponse> {
    const response = await operationalAuthenticatedFetch(`${this.baseUrl}/v1/cash-sessions/current`, {}, identity, this.fetcher);
    await requireSuccess(response);
    return getCurrentCashSessionResponseSchema.parse(await response.json());
  }

  /**
   * Cenários Gherkin "Divergência no fechamento", "Fechamento sem divergência" e "Mesa aberta no
   * fechamento" (US-055 §4). `authorizationToken` (RN-018) só é enviado quando há mesa ainda
   * aberta e o gerente já autorizou o contorno via {@link authorize}.
   */
  async close(
    identity: Readonly<OperationalRequestIdentity>,
    sessionId: string,
    input: CloseCashSessionRequest,
    authorizationToken?: string,
  ): Promise<CloseCashSessionResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/cash-sessions/${encodeURIComponent(sessionId)}/close`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
        },
        body: JSON.stringify(closeCashSessionRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return closeCashSessionResponseSchema.parse(await response.json());
  }

  /**
   * Sangria (`WITHDRAWAL`) ou suprimento (`SUPPLY`) — US-056 §4. `authorizationToken` só é enviado
   * quando a sangria ultrapassa o limite sem autorização e o gerente já autorizou.
   */
  async registerMovement(
    identity: Readonly<OperationalRequestIdentity>,
    input: RegisterCashMovementRequest,
    authorizationToken?: string,
  ): Promise<RegisterCashMovementResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/cash-sessions/movements`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
          ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
        },
        body: JSON.stringify(registerCashMovementRequestSchema.parse(input)),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return registerCashMovementResponseSchema.parse(await response.json());
  }

  /** Histórico do turno acessível na mesma tela (US-056 §7/§10). */
  async listMovements(identity: Readonly<OperationalRequestIdentity>): Promise<ListCashMovementsResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/cash-sessions/current/movements`,
      {},
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return listCashMovementsResponseSchema.parse(await response.json());
  }

  /**
   * Elevação pontual (ADR-023) — usada tanto para `CLOSE_DIVERGENT_CASH` (RN-018, mesa aberta no
   * fechamento) quanto para `WITHDRAWAL_ABOVE_LIMIT` (US-056 §5/RN-011, sangria acima do limite):
   * o gerente informa o próprio PIN no mesmo terminal, sem trocar de sessão.
   */
  async authorize(
    identity: Readonly<OperationalRequestIdentity>,
    input: { readonly action: 'CLOSE_DIVERGENT_CASH' | 'WITHDRAWAL_ABOVE_LIMIT'; readonly pin: string; readonly context?: Record<string, unknown> },
  ): Promise<AuthorizeCashActionResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/auth/authorize`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action: input.action, pin: input.pin, context: input.context ?? {} }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return response.json() as Promise<AuthorizeCashActionResponse>;
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string; meta?: Record<string, unknown> }
    | null;
  throw new CashSessionApiError(
    problem?.detail ?? 'Não foi possível concluir a operação.',
    problem?.code,
    problem?.meta,
  );
}
