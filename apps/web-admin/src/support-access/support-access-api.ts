import {
  supportAccessListResponseSchema,
  type SupportAccessListResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export interface SupportAccessApi {
  history(): Promise<SupportAccessListResponse>;
  revoke(id: string): Promise<void>;
}

/**
 * US-145 §10 — lado do cliente (`GET /v1/tenant/support-access-history`,
 * `DELETE /v1/tenant/support-access/{id}`). Mesmo padrão de `categories-api.ts`
 * (Idempotency-Key nova por chamada na revogação — ação destrutiva/idempotente por natureza).
 */
export class SupportAccessApiClient implements SupportAccessApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async history(): Promise<SupportAccessListResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/support-access-history`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return supportAccessListResponseSchema.parse(await response.json());
  }

  async revoke(id: string): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/tenant/support-access/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
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
