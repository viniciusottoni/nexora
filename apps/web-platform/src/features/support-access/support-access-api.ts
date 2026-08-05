import {
  grantSupportAccessResponseSchema,
  type GrantSupportAccessRequest,
  type GrantSupportAccessResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export interface SupportAccessApi {
  grant(tenantId: string, input: GrantSupportAccessRequest): Promise<GrantSupportAccessResponse>;
}

/**
 * US-145 §7 — `POST /v1/platform/tenants/{id}/support-access`. Mesmo padrão de
 * `tenants-api.ts` (Idempotency-Key gerada por intenção: mesmo corpo reusa a mesma chave até a
 * concessão ter sucesso, para que reenvio de rede não gere um segundo token).
 */
export function createSupportAccessApi(baseUrl = ''): SupportAccessApi {
  let pendingIntent: { body: string; key: string } | undefined;

  return {
    async grant(tenantId, input) {
      const body = JSON.stringify(input);
      if (!pendingIntent || pendingIntent.body !== body) {
        pendingIntent = { body, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/support-access`,
        {
          method: 'POST',
          credentials: 'include',
          headers: {
            'content-type': 'application/json',
            'idempotency-key': pendingIntent.key,
          },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = grantSupportAccessResponseSchema.parse(await response.json());
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
