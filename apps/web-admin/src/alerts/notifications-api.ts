import { alertListResponseSchema, type Alert } from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-081 (Entrega in-app e push de navegador) §7 — cliente HTTP do sino de notificações do
 * `TopBar` (ver `notification-bell.tsx`). Só a leitura de pendentes e o reconhecimento — central
 * completa (histórico, push VAPID) é de outro app/tarefa; aqui é só o suficiente para o sino do
 * web-admin não ficar mudo.
 */
export class NotificationsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async listUnread(): Promise<readonly Alert[]> {
    const response = await this.fetcher(`${this.baseUrl}/v1/notifications?status=unread`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return alertListResponseSchema.parse(await response.json()).alerts;
  }

  async acknowledge(id: string): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/alerts/${encodeURIComponent(id)}/acknowledge`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      },
    );
    await requireSuccess(response);
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
