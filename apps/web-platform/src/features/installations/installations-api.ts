import {
  installationDiagnosticsResponseSchema,
  installationIncidentListResponseSchema,
  platformInstallationListResponseSchema,
  type InstallationDiagnosticsResponse,
  type InstallationIncidentListResponse,
  type PlatformInstallationSummary,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-140 — cliente HTTP do painel de instalações com saúde. Só leitura (GET), por isso nenhum
 * método aqui carrega `Idempotency-Key` (ADR-020 só exige a chave em POST/PUT/PATCH/DELETE) —
 * diferente de `tenants-api.ts`, cujo `provision` escreve.
 */
export interface InstallationsApi {
  list(): Promise<readonly PlatformInstallationSummary[]>;
  diagnostics(installationId: string): Promise<InstallationDiagnosticsResponse>;
  incidents(installationId: string): Promise<InstallationIncidentListResponse['data']>;
}

export function createInstallationsApi(baseUrl = ''): InstallationsApi {
  return {
    async list() {
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/installations`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      return platformInstallationListResponseSchema.parse(await response.json()).data;
    },

    async diagnostics(installationId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/installations/${encodeURIComponent(installationId)}/diagnostics`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return installationDiagnosticsResponseSchema.parse(await response.json());
    },

    async incidents(installationId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/installations/${encodeURIComponent(installationId)}/incidents`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return installationIncidentListResponseSchema.parse(await response.json()).data;
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
