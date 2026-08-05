import { platformSummaryResponseSchema, type PlatformSummaryResponse } from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-150 §7 — cliente HTTP do resumo da plataforma (`GET /v1/platform/summary`). Também serve como
 * "sonda" de autorização do shell (ver `PlatformAdminGate` em `app.tsx`): é a primeira chamada
 * protegida feita após o login, então seu status HTTP (200/401/403) é o que decide se o
 * administrador está autorizado — nunca a claim do token isolada (ADR-021: 403 nunca ganha
 * fallback para rota menos protegida).
 */
export interface PlatformSummaryApi {
  get(): Promise<PlatformSummaryResponse>;
}

export function createPlatformSummaryApi(baseUrl = ''): PlatformSummaryApi {
  return {
    async get() {
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/summary`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      return platformSummaryResponseSchema.parse(await response.json());
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
