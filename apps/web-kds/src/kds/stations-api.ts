import { stationListResponseSchema, type StationDto, type StationListResponse } from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/**
 * Paleta de cor permitida no cadastro de praça — MESMA regra de
 * `apps/web-admin/src/stations/stations-api.ts` (`STATION_COLOR_OPTIONS`/`STATION_COLOR_CSS_VAR`).
 * Duplicado aqui (em vez de importado) porque cada app do monorepo é independente — `web-kds` não
 * depende de `web-admin` — mas a CHAVE semântica e o token CSS de destino são os mesmos
 * (`packages/ui/src/tokens/colors.css`), então a cor de uma praça é sempre a mesma em qualquer tela
 * (ADR-010: nenhuma cor literal, US-017 §10: "cor por praça usada consistentemente no KDS...").
 */
export const STATION_COLOR_CSS_VAR: Record<string, string> = {
  navy: 'var(--nx-navy-500)',
  blue: 'var(--nx-blue-500)',
  cyan: 'var(--nx-cyan-500)',
  teal: 'var(--nx-teal-500)',
  green: 'var(--nx-green-500)',
  amber: 'var(--nx-warning-500)',
  red: 'var(--nx-danger-500)',
  gray: 'var(--nx-gray-500)',
};

export function stationColorCssValue(color: string | null | undefined): string {
  if (color && color in STATION_COLOR_CSS_VAR) return STATION_COLOR_CSS_VAR[color]!;
  return 'var(--text-inverse, #fff)';
}

/** Erro de API com o `code` estável do RFC 7807 (ADR-021) preservado — mesmo padrão de `KdsApiError` em `kds-queue-api.ts`. */
export class StationsApiError extends Error {
  constructor(
    message: string,
    readonly code: string | undefined,
  ) {
    super(message);
    this.name = 'StationsApiError';
  }
}

/**
 * Cliente HTTP de leitura de praças de produção (US-042, também US-017) —
 * `GET /v1/catalog/stations`, porta de `Nexora.Api.Cloud/Controllers/StationsController.cs`. A
 * gravação da preferência de filtro por dispositivo (`PATCH /v1/devices/{id}/preferences`) NÃO
 * mora aqui — reusa `DevicePreferencesApi` (`./device-preferences-api.js`), o mesmo cliente que
 * US-045 (som) já usa para a mesma sub-chave `kds`, em vez de duplicar outro cliente HTTP para o
 * mesmo endpoint. Mesmo padrão de autenticação de `KdsQueueApi` (`kds-queue-api.ts`):
 * `operationalAuthenticatedFetch` (Bearer + cabeçalhos de dispositivo), nunca `authenticatedFetch`
 * de sessão de usuário — o terminal do KDS não tem usuário logado, só o par dispositivo+PIN.
 *
 * ATENÇÃO (achado durante a US-042): `GET /v1/catalog/stations` hoje só existe em
 * `Nexora.Api.Cloud`, não em `Nexora.Api.Edge` — o KDS roda no edge (mesma origem, `baseUrl`
 * vazio, como os demais clientes deste app). Se o terminal não alcançar a nuvem (offline, ou o edge
 * não expõe essa rota), `list()` falha e `useStationFilter` degrada para o modo "praça única"
 * (filtro oculto, comportamento herdado da claim `stn` do token) — nunca trava a tela. Recomendação
 * para quem for fechar essa lacuna: espelhar um endpoint de LEITURA em `Nexora.Api.Edge` sobre a
 * cópia local de `Station` (mesma régua de "cardápio editado na nuvem, lido no local").
 */
export class KdsStationsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  async list(identity: Readonly<OperationalRequestIdentity>): Promise<StationListResponse> {
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/catalog/stations`,
      { credentials: 'include' },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return stationListResponseSchema.parse(await response.json());
  }
}

export type { StationDto };

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string }
    | null;
  throw new StationsApiError(
    problem?.detail ?? 'Não foi possível carregar as praças de produção.',
    problem?.code,
  );
}
