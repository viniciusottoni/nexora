import {
  administrativeTimelineListResponseSchema,
  type AdministrativeTimelineFilters,
  type AdministrativeTimelineListResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — cliente HTTP autocontido de
 * `GET /v1/platform/tenants/{id}/administrative-timeline`, mesmo padrão de `tenant-plan-api.ts`.
 */
export interface TenantAdministrativeTimelineApi {
  list(
    tenantId: string,
    filters?: AdministrativeTimelineFilters,
  ): Promise<AdministrativeTimelineListResponse>;
}

export function createTenantAdministrativeTimelineApi(
  baseUrl = '',
): TenantAdministrativeTimelineApi {
  return {
    async list(tenantId, filters) {
      const params = new URLSearchParams();
      filters?.type?.forEach((value) => params.append('type', value));
      if (filters?.from) params.set('from', filters.from);
      if (filters?.to) params.set('to', filters.to);
      if (filters?.actorId) params.set('actorId', filters.actorId);
      if (filters?.correlationId) params.set('correlationId', filters.correlationId);
      if (filters?.limit !== undefined) params.set('limit', String(filters.limit));
      if (filters?.cursor) params.set('cursor', filters.cursor);
      const query = params.toString();

      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/administrative-timeline${query ? `?${query}` : ''}`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return administrativeTimelineListResponseSchema.parse(await response.json());
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
