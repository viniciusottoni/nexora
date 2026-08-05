import {
  publishReleaseResponseSchema,
  releaseRolloutResponseSchema,
  type PublishReleaseRequest,
  type PublishReleaseResponse,
  type ReleaseRolloutResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-146 (Atualização controlada do parque) — cliente HTTP de publicação de release e progresso
 * de rollout. `publish` carrega `Idempotency-Key` (ADR-020, POST); `rollout` é só leitura (GET).
 */
export interface ReleasesApi {
  publish(input: PublishReleaseRequest): Promise<PublishReleaseResponse>;
  rollout(version: string): Promise<ReleaseRolloutResponse>;
}

export function createReleasesApi(baseUrl = ''): ReleasesApi {
  let pendingIntent: { body: string; key: string } | undefined;

  return {
    async publish(input) {
      const body = JSON.stringify(input);
      if (!pendingIntent || pendingIntent.body !== body) {
        pendingIntent = { body, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/releases`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'content-type': 'application/json',
          'idempotency-key': pendingIntent.key,
        },
        body,
      });
      if (!response.ok) throw await toApiError(response);
      const result = publishReleaseResponseSchema.parse(await response.json());
      pendingIntent = undefined;
      return result;
    },

    async rollout(version) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/releases/${encodeURIComponent(version)}/rollout`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return releaseRolloutResponseSchema.parse(await response.json());
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
