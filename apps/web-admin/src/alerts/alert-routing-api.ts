import {
  alertRoutingConfigSchema,
  updateAlertRoutingRequestSchema,
  type AlertRoutingConfig,
  type UpdateAlertRoutingRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-082 (Direcionamento por perfil e por ação) §7 — cliente HTTP de
 * `GET/PATCH /v1/tenant/alert-routing` (Nexora.Api.Cloud, policy `config:write`). O GET já vem
 * 100% resolvido (padrão ou customizado, um por tipo de alerta do catálogo fixo — ver
 * `alertEngineTypes`); o PATCH é um dicionário parcial, um tipo de alerta por vez, com só os
 * campos daquela regra que o gestor editou.
 */
export class AlertRoutingApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async get(): Promise<AlertRoutingConfig> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/alert-routing`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return alertRoutingConfigSchema.parse(await response.json());
  }

  async update(input: UpdateAlertRoutingRequest): Promise<AlertRoutingConfig> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/alert-routing`, {
      method: 'PATCH',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
      },
      body: JSON.stringify(updateAlertRoutingRequestSchema.parse(input)),
    });
    await requireSuccess(response);
    return alertRoutingConfigSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível salvar o direcionamento de alertas.');
}
