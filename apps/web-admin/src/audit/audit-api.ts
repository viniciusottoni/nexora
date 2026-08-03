import {
  auditLogListResponseSchema,
  type AuditLogFilters,
  type AuditLogListResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-091 (Consulta e filtro da trilha) — cliente HTTP de `GET /v1/audit` (Nexora.Api.Cloud, policy
 * `AuditRead`). Mesmo estilo de `PricingApi`: classe fina, filtros viram querystring (chaves
 * `undefined`/vazias são omitidas — não faz sentido mandar `?actorId=` vazio), resposta validada
 * pelo schema Zod do contrato antes de voltar ao componente.
 */
export class AuditApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(filters: AuditLogFilters = {}): Promise<AuditLogListResponse> {
    const query = toQueryString(filters);
    const response = await this.fetcher(`${this.baseUrl}/v1/audit${query}`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return auditLogListResponseSchema.parse(await response.json());
  }
}

/** Monta `?a=1&b=2` a partir dos filtros — omite chaves `undefined`, `null` ou string vazia. */
function toQueryString(filters: AuditLogFilters): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value === undefined || value === null || value === '') continue;
    params.set(key, String(value));
  }
  const serialized = params.toString();
  return serialized ? `?${serialized}` : '';
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível consultar a trilha de auditoria.');
}
