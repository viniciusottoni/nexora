import {
  tenantThresholdsSchema,
  updateTenantThresholdsRequestSchema,
  type TenantThresholds,
  type UpdateTenantThresholdsRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-080 (Motor de alertas com limiares configuráveis) §7 — cliente HTTP de
 * `GET/PATCH /v1/tenant/thresholds` (Nexora.Api.Cloud, policy `config:write` no PATCH — mesma
 * policy de branding). Mesmo estilo de `StationsApi`/`RolesApi`: classe fina, corpo do PATCH é
 * parcial (só os campos alterados), resposta sempre validada pelo schema Zod antes de voltar ao
 * componente.
 */
export class ThresholdsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async get(): Promise<TenantThresholds> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/thresholds`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return tenantThresholdsSchema.parse(await response.json());
  }

  async update(input: UpdateTenantThresholdsRequest): Promise<TenantThresholds> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/thresholds`, {
      method: 'PATCH',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
      },
      body: JSON.stringify(updateTenantThresholdsRequestSchema.parse(input)),
    });
    await requireSuccess(response);
    return tenantThresholdsSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível salvar os limiares de alerta.');
}
