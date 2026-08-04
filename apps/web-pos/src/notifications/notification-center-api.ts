import {
  alertListResponseSchema,
  alertSchema,
  type Alert,
  type AlertListResponse,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

// Ver comentário equivalente em table-map-api.ts/availability-api.ts — `fetch` capturado bruto e
// chamado depois como `this.fetcher(...)` quebra em navegador real ("Illegal invocation").
const browserFetch: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args);

/**
 * Cliente HTTP da central de notificações (US-081 §7, US-083) — `GET /v1/notifications?status=unread`
 * (mesmo formato de `GET /v1/alerts`, já filtrado para o usuário autenticado via papel/targetUserId)
 * e `POST /v1/alerts/{id}/acknowledge`. Mesmo padrão de `TableMapApi`/`AvailabilityApi`: fetcher
 * injetável para teste, `Idempotency-Key` nova em toda escrita (ADR-020).
 *
 * [DECISÃO] Consome o endpoint UNGROUPED (`status=unread`), não `grouped=true` — a instrução desta
 * história pede explicitamente o primeiro. Isso funciona porque uma rajada agrupada (US-083) já
 * chega aqui como UMA linha por grupo, com `message` já consolidada pelo backend ("5 pedidos
 * atrasados") — o cliente nunca precisa somar contagens localmente.
 */
export class NotificationCenterApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = browserFetch,
  ) {}

  async listUnread(identity: Readonly<OperationalRequestIdentity>): Promise<AlertListResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/notifications?status=unread`,
      { credentials: 'include' },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return alertListResponseSchema.parse(await response.json());
  }

  /** US-081 §4, cenário "Reconhecimento" — devolve o alerta atualizado (com `acknowledgedAt`). */
  async acknowledge(identity: Readonly<OperationalRequestIdentity>, alertId: string): Promise<Alert> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/alerts/${encodeURIComponent(alertId)}/acknowledge`,
      { method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() } },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return alertSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
