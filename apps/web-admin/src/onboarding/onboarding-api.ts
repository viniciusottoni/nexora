import {
  onboardingStatusResponseSchema,
  type OnboardingStatusResponse,
  type OnboardingStepKey,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export class OnboardingApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string | undefined,
    message: string,
  ) {
    super(message);
  }
}

/**
 * Cliente HTTP do roteiro de implantação (US-141) no painel do cliente — mesmo padrão de
 * `DevicesApi`/`AvailabilityApi` (fetcher injetável para teste, `Idempotency-Key` na escrita,
 * ADR-020). Diferente do painel da Replay (`web-platform`), este cliente nunca chama
 * `POST .../activate` — só a Replay ativa (US-141 §1 "para que a Replay implante em escala"; ver
 * `OnboardingController.Activate`, restrito à policy `PlatformAdmin`).
 */
export class OnboardingApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async getStatus(tenantId: string): Promise<OnboardingStatusResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/onboarding`,
      { credentials: 'include' },
    );
    await requireSuccess(response);
    return onboardingStatusResponseSchema.parse(await response.json());
  }

  /** Conclusão manual de um passo (US-141 §3.1 "assistente de configuração inicial") — usado ao menos por TRAINING/PILOT. */
  async completeStep(tenantId: string, key: OnboardingStepKey): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/onboarding/${key}`,
      {
        method: 'PATCH',
        credentials: 'include',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      },
    );
    await requireSuccess(response);
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as {
    code?: string;
    detail?: string;
  } | null;
  throw new OnboardingApiError(
    response.status,
    problem?.code,
    problem?.detail ?? 'Não foi possível concluir a operação.',
  );
}
