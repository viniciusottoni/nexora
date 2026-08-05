import {
  businessTemplateDetailResponseSchema,
  businessTemplateListResponseSchema,
  type BusinessTemplateDetailResponse,
  type BusinessTemplateSummary,
  type UpdateBusinessTemplateRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * Manutenção do catálogo de modelos de negócio pela Replay (US-142 §10) —
 * `GET/PUT /v1/platform/templates[/{code}]`, mesma convenção de `tenants-api.ts`
 * (idempotency-key gerado por intenção de edição, não por tentativa).
 */
export interface BusinessTemplatesApi {
  list(): Promise<BusinessTemplateSummary[]>;
  get(code: string): Promise<BusinessTemplateDetailResponse>;
  update(code: string, input: UpdateBusinessTemplateRequest): Promise<BusinessTemplateDetailResponse>;
}

export function createBusinessTemplatesApi(baseUrl = ''): BusinessTemplatesApi {
  let pendingIntent: { code: string; body: string; key: string } | undefined;

  return {
    async list() {
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/templates`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      return businessTemplateListResponseSchema.parse(await response.json()).data;
    },

    async get(code) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/templates/${encodeURIComponent(code)}`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return businessTemplateDetailResponseSchema.parse(await response.json());
    },

    async update(code, input) {
      const body = JSON.stringify(input);
      if (!pendingIntent || pendingIntent.code !== code || pendingIntent.body !== body) {
        pendingIntent = { code, body, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/templates/${encodeURIComponent(code)}`,
        {
          method: 'PUT',
          credentials: 'include',
          headers: { 'content-type': 'application/json', 'idempotency-key': pendingIntent.key },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      pendingIntent = undefined;
      return businessTemplateDetailResponseSchema.parse(await response.json());
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
