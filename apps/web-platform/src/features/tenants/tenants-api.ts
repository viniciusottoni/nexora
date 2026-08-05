import {
  businessTemplateListResponseSchema,
  createTenantResponseSchema,
  slugAvailabilityResponseSchema,
  type BusinessTemplateSummary,
  type CreateTenantRequest,
  type CreateTenantResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export interface TenantsApi {
  checkSlug(slug: string): Promise<boolean>;
  provision(input: CreateTenantRequest): Promise<CreateTenantResponse>;
  /** US-142: modelos ativos para o seletor da tela de provisionamento (`GET /v1/platform/templates`). */
  listTemplates(): Promise<BusinessTemplateSummary[]>;
}

export function createTenantsApi(baseUrl = ''): TenantsApi {
  let pendingIntent: { body: string; key: string } | undefined;

  return {
    async checkSlug(slug) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/slug-availability?slug=${encodeURIComponent(slug)}`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return slugAvailabilityResponseSchema.parse(await response.json()).available;
    },

    async listTemplates() {
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/templates`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      return businessTemplateListResponseSchema.parse(await response.json()).data;
    },

    async provision(input) {
      const body = JSON.stringify(input);
      if (!pendingIntent || pendingIntent.body !== body) {
        pendingIntent = { body, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/tenants`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'content-type': 'application/json',
          'idempotency-key': pendingIntent.key,
        },
        body,
      });
      if (!response.ok) throw await toApiError(response);
      const result = createTenantResponseSchema.parse(await response.json());
      pendingIntent = undefined;
      return result;
    },
  };
}

export interface ApiProblem extends Error {
  code?: string;
}

async function toApiError(response: Response): Promise<ApiProblem> {
  const payload = (await response.json().catch(() => undefined)) as
    { detail?: string; code?: string } | undefined;
  const error = new Error(payload?.detail ?? 'Não foi possível concluir a operação.') as ApiProblem;
  if (payload?.code) error.code = payload.code;
  return error;
}
