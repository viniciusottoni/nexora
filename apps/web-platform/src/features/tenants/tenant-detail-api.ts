import {
  tenantOverviewResponseSchema,
  tenantStatusTransitionResponseSchema,
  type TenantOverviewResponse,
  type TenantStatusTransitionResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-153 · Ciclo de vida do estabelecimento — subconjunto do enum canônico que a ação
 * administrativa manual pode alvejar (`PROVISIONED`/`INSTALLING` só são alcançados pelo fluxo
 * automático de provisionamento/instalação, nunca por este botão). A UI nunca decide sozinha
 * QUAIS transições são legais — isso vem de `overview.tenant.availableTransitions` — só usa este
 * tipo para rotular o botão e tipar o corpo da requisição.
 */
export type TenantStatusTransitionTarget = 'ACTIVE' | 'SUSPENDED' | 'CANCELLED';

const TRANSITION_TARGETS: readonly TenantStatusTransitionTarget[] = ['ACTIVE', 'SUSPENDED', 'CANCELLED'];

export function isTenantStatusTransitionTarget(status: string): status is TenantStatusTransitionTarget {
  return (TRANSITION_TARGETS as readonly string[]).includes(status);
}

export interface TenantStatusTransitionInput {
  readonly targetStatus: TenantStatusTransitionTarget;
  readonly reason: string;
  readonly effectiveAt?: string;
}

/**
 * US-152 · Visão 360 e acesso aos módulos do estabelecimento — cliente HTTP de
 * `GET /v1/platform/tenants/{id}/overview`. Só leitura (GET), por isso `get` não carrega
 * `Idempotency-Key` (ADR-020 só exige a chave em POST/PUT/PATCH/DELETE) — mesmo padrão de
 * `installations-api.ts`.
 *
 * US-153 · Ciclo de vida do estabelecimento — `transitionStatus` é a única escrita desta porta:
 * `POST /v1/platform/tenants/{id}/status-transitions`, com `Idempotency-Key` por intenção (mesmo
 * padrão de `tenants-api.ts` `provision()`) e `If-Match` com o `statusVersion` corrente (controle
 * de concorrência otimista, ADR-020/ADR-023).
 */
export interface TenantOverviewApi {
  get(tenantId: string): Promise<TenantOverviewResponse>;
  transitionStatus(
    tenantId: string,
    statusVersion: number,
    input: TenantStatusTransitionInput,
  ): Promise<TenantStatusTransitionResponse>;
}

export function createTenantOverviewApi(baseUrl = ''): TenantOverviewApi {
  let pendingTransition: { intent: string; key: string } | undefined;

  return {
    async get(tenantId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/overview`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return tenantOverviewResponseSchema.parse(await response.json());
    },

    async transitionStatus(tenantId, statusVersion, input) {
      const body = JSON.stringify(input);
      const intent = `${tenantId}:${body}`;
      if (!pendingTransition || pendingTransition.intent !== intent) {
        pendingTransition = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/status-transitions`,
        {
          method: 'POST',
          credentials: 'include',
          headers: {
            'content-type': 'application/json',
            'idempotency-key': pendingTransition.key,
            'if-match': `"${statusVersion}"`,
          },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      return tenantStatusTransitionResponseSchema.parse(await response.json());
    },
  };
}

export interface ApiProblem extends Error {
  code?: string;
  status?: number;
}

async function toApiError(response: Response): Promise<ApiProblem> {
  const payload = (await response.json().catch(() => undefined)) as
    { detail?: string; code?: string } | undefined;
  const error = new Error(payload?.detail ?? 'Não foi possível concluir a operação.') as ApiProblem;
  if (payload?.code) error.code = payload.code;
  error.status = response.status;
  return error;
}
