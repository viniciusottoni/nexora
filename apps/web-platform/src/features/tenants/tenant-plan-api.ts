import { authenticatedFetch } from '@nexora/ui';

/**
 * US-154 · Gestão de planos e configuração comercial — cliente HTTP autocontido de
 * `GET /v1/platform/plans`, `GET /v1/platform/tenants/{id}/plan`,
 * `PUT /v1/platform/tenants/{id}/plan` e `POST /v1/platform/tenants/{id}/plan/reconciliations`.
 *
 * Tipos definidos LOCALMENTE (não importados de `@nexora/contracts`) de propósito: o schema
 * espelho já existe em `packages/contracts/src/tenant-plan.ts`, mas o pacote ainda não o exporta
 * (`index.ts` só ganha `export * from './tenant-plan.js';` numa integração central posterior — ver
 * relatório final da tarefa). Até lá, `tenant-plan-section.tsx` precisa ser autocontido para não
 * quebrar o build de outras histórias em paralelo.
 */
export interface PlatformPlanSummary {
  readonly code: string;
  readonly name: string;
  readonly active: boolean;
  readonly capabilities: readonly string[];
}

export interface TenantPlanScheduled {
  readonly plan: string;
  readonly effectiveAt: string;
}

export interface TenantPlanHistoryEntry {
  readonly previous: string;
  readonly next: string;
  readonly requestedAt: string;
  readonly effectiveAt: string;
  readonly appliedAt: string | null;
  readonly reason: string;
  readonly actorId: string | null;
}

export interface TenantPlanView {
  readonly current: string;
  readonly effectiveCapabilities: readonly string[];
  readonly scheduled: TenantPlanScheduled | null;
  readonly consistent: boolean;
  readonly version: number;
  readonly history: readonly TenantPlanHistoryEntry[];
}

export interface TenantPlanUpdateInput {
  readonly plan: string;
  readonly effectiveAt?: string;
  readonly reason: string;
}

export interface TenantPlanUpdateResult {
  readonly current: string;
  readonly scheduled: TenantPlanScheduled | null;
  readonly version: number;
}

export interface TenantPlanReconcileResult {
  readonly current: string;
  readonly effectiveCapabilities: readonly string[];
  readonly consistent: boolean;
  readonly changed: boolean;
}

export interface ApiProblem extends Error {
  code?: string;
  status?: number;
}

export interface TenantPlanApi {
  /** Catálogo completo (ativos e inativos) — só GET, sem `Idempotency-Key` (ADR-020 exige a chave só em escrita). */
  listPlans(): Promise<PlatformPlanSummary[]>;
  get(tenantId: string): Promise<TenantPlanView>;
  /** `PUT` com `Idempotency-Key` por intenção e `If-Match` com a versão corrente — mesmo padrão de `tenant-detail-api.ts` `transitionStatus`. */
  update(
    tenantId: string,
    version: number,
    input: TenantPlanUpdateInput,
  ): Promise<TenantPlanUpdateResult>;
  /** Endpoint adicional (ver docstring do backend) que corrige a divergência detectada — idempotente por conteúdo, `Idempotency-Key` protege contra reenvio de rede duplicado. */
  reconcile(tenantId: string): Promise<TenantPlanReconcileResult>;
}

export function createTenantPlanApi(baseUrl = ''): TenantPlanApi {
  let pendingUpdate: { intent: string; key: string } | undefined;
  let pendingReconcile: { intent: string; key: string } | undefined;

  return {
    async listPlans() {
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/plans`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      const payload = (await response.json()) as { data: PlatformPlanSummary[] };
      return payload.data;
    },

    async get(tenantId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/plan`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return (await response.json()) as TenantPlanView;
    },

    async update(tenantId, version, input) {
      const body = JSON.stringify(input);
      const intent = `${tenantId}:${version}:${body}`;
      if (!pendingUpdate || pendingUpdate.intent !== intent) {
        pendingUpdate = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/plan`,
        {
          method: 'PUT',
          credentials: 'include',
          headers: {
            'content-type': 'application/json',
            'idempotency-key': pendingUpdate.key,
            'if-match': `"${version}"`,
          },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = (await response.json()) as TenantPlanUpdateResult;
      pendingUpdate = undefined;
      return result;
    },

    async reconcile(tenantId) {
      const intent = tenantId;
      if (!pendingReconcile || pendingReconcile.intent !== intent) {
        pendingReconcile = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/plan/reconciliations`,
        {
          method: 'POST',
          credentials: 'include',
          headers: { 'idempotency-key': pendingReconcile.key },
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = (await response.json()) as TenantPlanReconcileResult;
      pendingReconcile = undefined;
      return result;
    },
  };
}

async function toApiError(response: Response): Promise<ApiProblem> {
  const payload = (await response.json().catch(() => undefined)) as
    { detail?: string; code?: string } | undefined;
  const error = new Error(payload?.detail ?? 'Não foi possível concluir a operação.') as ApiProblem;
  if (payload?.code) error.code = payload.code;
  error.status = response.status;
  return error;
}
