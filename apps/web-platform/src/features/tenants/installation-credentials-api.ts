import { authenticatedFetch } from '@nexora/ui';

/**
 * US-156 · Recuperação do provisionamento e token de instalação — cliente HTTP de
 * `GET /v1/platform/tenants/{tenantId}/deployment`,
 * `POST /v1/platform/installations/{installationId}/tokens` e
 * `DELETE /v1/platform/installations/{installationId}/tokens/{credentialId}`.
 *
 * Tipos LOCAIS (não importados de `@nexora/contracts`) de propósito — ver o cabeçalho de
 * `packages/contracts/src/installation-credentials.ts` (arquivo NOVO, mas ainda não reexportado
 * pelo barril `index.ts`, fora do escopo desta tarefa por disciplina de isolamento de arquivos):
 * um import profundo (`@nexora/contracts/installation-credentials`) não resolveria em runtime hoje
 * (`package.json` só publica `.` no campo `exports`). Os shapes abaixo são espelho campo a campo do
 * contrato C# (`Nexora.Contracts.Tenants.TenantDeploymentStatusResponse`/
 * `Nexora.Contracts.Platform.ReissueInstallationTokenResponse`) e do arquivo de contracts TS já
 * escrito — quando a integração central acrescentar a linha de reexport, este arquivo pode trocar
 * para o import compartilhado sem mudar nenhum comportamento observável.
 */
export type TenantDeploymentInstallationStatus = 'PENDING' | 'ACTIVE' | 'OFFLINE';

export interface TenantDeploymentInstallation {
  readonly id: string;
  readonly status: TenantDeploymentInstallationStatus;
  readonly canReissueToken: boolean;
}

export interface TenantDeploymentStatus {
  readonly completed: number;
  readonly total: number;
  readonly installation: TenantDeploymentInstallation | null;
  readonly nextAction: string | null;
}

export interface ReissueInstallationTokenInput {
  readonly reason: string;
  readonly expiresInHours: number;
}

/**
 * `installToken`/`installCommand` nulos = repetição idempotente da mesma intenção (ver docstring
 * de `reissueInstallationTokenResponseSchema` em `packages/contracts/src/installation-credentials.ts`)
 * — o segredo já foi mostrado uma vez e, por definição de exibição única, não volta a aparecer.
 */
export interface ReissueInstallationTokenResult {
  readonly credentialId: string;
  readonly expiresAt: string;
  readonly installToken: string | null;
  readonly installCommand: string | null;
}

export interface InstallationCredentialsApi {
  getDeploymentStatus(tenantId: string): Promise<TenantDeploymentStatus>;
  reissueToken(
    installationId: string,
    input: ReissueInstallationTokenInput,
  ): Promise<ReissueInstallationTokenResult>;
  revokeCredential(installationId: string, credentialId: string, reason: string): Promise<void>;
}

export function createInstallationCredentialsApi(baseUrl = ''): InstallationCredentialsApi {
  return {
    async getDeploymentStatus(tenantId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/deployment`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return (await response.json()) as TenantDeploymentStatus;
    },

    async reissueToken(installationId, input) {
      // Cada clique em "reemitir" é uma intenção NOVA (ao contrário do padrão de
      // "reenviar a mesma intenção" de tenants-api.ts `provision()`/tenant-detail-api.ts
      // `transitionStatus()`) — uma chave nova por chamada é o comportamento certo aqui: o
      // administrador que reemite duas vezes seguidas (ex.: "ainda não copiei, preciso de outra")
      // quer DUAS rotações de verdade, não a mesma intenção idempotente repetida.
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/installations/${encodeURIComponent(installationId)}/tokens`,
        {
          method: 'POST',
          credentials: 'include',
          headers: {
            'content-type': 'application/json',
            'idempotency-key': crypto.randomUUID(),
          },
          body: JSON.stringify(input),
        },
      );
      if (!response.ok) throw await toApiError(response);
      return (await response.json()) as ReissueInstallationTokenResult;
    },

    async revokeCredential(installationId, credentialId, reason) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/installations/${encodeURIComponent(installationId)}/tokens/${encodeURIComponent(credentialId)}`,
        {
          method: 'DELETE',
          credentials: 'include',
          headers: {
            'content-type': 'application/json',
            'idempotency-key': crypto.randomUUID(),
          },
          body: JSON.stringify({ reason }),
        },
      );
      if (!response.ok) throw await toApiError(response);
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
