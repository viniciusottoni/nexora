import {
  businessTemplateListResponseSchema,
  createTenantResponseSchema,
  slugAvailabilityResponseSchema,
  tenantDirectoryResponseSchema,
  type BusinessTemplateSummary,
  type CreateTenantRequest,
  type CreateTenantResponse,
  type TenantDirectoryQuery,
  type TenantDirectoryResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export interface TenantsApi {
  checkSlug(slug: string): Promise<boolean>;
  provision(input: CreateTenantRequest): Promise<CreateTenantResponse>;
  /** US-142: modelos ativos para o seletor da tela de provisionamento (`GET /v1/platform/templates`). */
  listTemplates(): Promise<BusinessTemplateSummary[]>;
  /**
   * US-151 · Diretório de estabelecimentos com busca e filtros — `GET /v1/platform/tenants` com
   * busca textual, filtros repetíveis, ordenação e paginação por cursor. Substitui o `list()` sem
   * parâmetros da US-150 (diretório mínimo).
   */
  search(query: TenantDirectoryQuery): Promise<TenantDirectoryResponse>;
}

/** Monta a querystring do diretório — arrays viram múltiplos params repetidos (`status=A&status=B`). */
export function buildTenantDirectoryQueryString(query: Readonly<TenantDirectoryQuery>): string {
  const params = new URLSearchParams();
  if (query.query) params.set('query', query.query);
  for (const value of query.status ?? []) params.append('status', value);
  for (const value of query.plan ?? []) params.append('plan', value);
  for (const value of query.template ?? []) params.append('template', value);
  for (const value of query.health ?? []) params.append('health', value);
  if (query.createdFrom) params.set('createdFrom', query.createdFrom);
  if (query.createdTo) params.set('createdTo', query.createdTo);
  params.set('sort', query.sort ?? 'attention');
  params.set('limit', String(query.limit ?? 25));
  if (query.cursor) params.set('cursor', query.cursor);
  return params.toString();
}

export function createTenantsApi(baseUrl = ''): TenantsApi {
  let pendingIntent: { body: string; key: string } | undefined;

  return {
    async search(query) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants?${buildTenantDirectoryQueryString(query)}`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return tenantDirectoryResponseSchema.parse(await response.json());
    },

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
