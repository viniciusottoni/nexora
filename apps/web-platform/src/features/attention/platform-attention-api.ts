import {
  attentionQueueListResponseSchema,
  attentionAcknowledgementResponseSchema,
  type AcknowledgeAttentionItemRequest,
  type AttentionAcknowledgementResponse,
  type AttentionQueueListResponse,
  type AttentionSeverity,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — cliente HTTP autocontido de
 * `GET /v1/platform/attention`, `POST /v1/platform/attention/{itemId}/acknowledgements` (com
 * `Idempotency-Key` por intenção, ADR-020, mesmo padrão de `support-access-api.ts`) e
 * `GET /v1/platform/attention/export` (baixa o CSV como `Blob`, dispara o download no navegador —
 * não há schema Zod para um arquivo binário).
 */
export interface AttentionQueueFilters {
  readonly severity?: readonly AttentionSeverity[];
  readonly limit?: number;
  readonly cursor?: string;
}

export interface PlatformAttentionApi {
  list(filters?: AttentionQueueFilters): Promise<AttentionQueueListResponse>;
  acknowledge(
    itemId: string,
    input: AcknowledgeAttentionItemRequest,
  ): Promise<AttentionAcknowledgementResponse>;
  exportCsv(filters?: AttentionQueueFilters): Promise<Blob>;
}

export function createPlatformAttentionApi(baseUrl = ''): PlatformAttentionApi {
  let pendingAck: { itemId: string; body: string; key: string } | undefined;

  return {
    async list(filters) {
      const query = buildQuery(filters);
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/attention${query}`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      return attentionQueueListResponseSchema.parse(await response.json());
    },

    async acknowledge(itemId, input) {
      const body = JSON.stringify(input);
      if (!pendingAck || pendingAck.itemId !== itemId || pendingAck.body !== body) {
        pendingAck = { itemId, body, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/attention/${encodeURIComponent(itemId)}/acknowledgements`,
        {
          method: 'POST',
          credentials: 'include',
          headers: {
            'content-type': 'application/json',
            'idempotency-key': pendingAck.key,
          },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = attentionAcknowledgementResponseSchema.parse(await response.json());
      pendingAck = undefined;
      return result;
    },

    async exportCsv(filters) {
      const query = buildQuery(filters);
      const response = await authenticatedFetch(`${baseUrl}/v1/platform/attention/export${query}`, {
        credentials: 'include',
      });
      if (!response.ok) throw await toApiError(response);
      return response.blob();
    },
  };
}

function buildQuery(filters: AttentionQueueFilters | undefined): string {
  if (!filters) return '';
  const params = new URLSearchParams();
  filters.severity?.forEach((value) => params.append('severity', value));
  if (filters.limit !== undefined) params.set('limit', String(filters.limit));
  if (filters.cursor) params.set('cursor', filters.cursor);
  const query = params.toString();
  return query ? `?${query}` : '';
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
