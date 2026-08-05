import {
  onboardingActivationPendingMetaSchema,
  onboardingStatusResponseSchema,
  type OnboardingStatusResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export interface OnboardingApi {
  getStatus(tenantId: string): Promise<OnboardingStatusResponse>;
  activate(tenantId: string): Promise<void>;
}

export interface OnboardingApiProblem extends Error {
  code?: string;
  /** Chaves de passo pendentes (US-141 §7 `meta.pending`) — populado só no 422 `ONBOARDING_INCOMPLETE`. */
  pendingSteps?: readonly string[];
}

export function createOnboardingApi(baseUrl = ''): OnboardingApi {
  return {
    async getStatus(tenantId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/onboarding`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return onboardingStatusResponseSchema.parse(await response.json());
    },

    async activate(tenantId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/activate`,
        {
          method: 'POST',
          credentials: 'include',
          headers: { 'idempotency-key': crypto.randomUUID() },
        },
      );
      if (!response.ok) throw await toApiError(response);
    },
  };
}

async function toApiError(response: Response): Promise<OnboardingApiProblem> {
  const payload = (await response.json().catch(() => undefined)) as
    | { detail?: string; code?: string; meta?: unknown }
    | undefined;
  const error = new Error(
    payload?.detail ?? 'Não foi possível concluir a operação.',
  ) as OnboardingApiProblem;
  if (payload?.code) error.code = payload.code;

  const meta = onboardingActivationPendingMetaSchema.safeParse(payload?.meta);
  if (meta.success) {
    error.pendingSteps = meta.data.pendingItems ?? meta.data.pending ?? [];
  }

  return error;
}
