import {
  stationListResponseSchema,
  stationSchema,
  type CreateStationRequest,
  type StationDto,
  type StationListResponse,
  type UpdateStationRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * Paleta de cor permitida no cadastro de praça (US-017 §10: "cor por praça usada
 * consistentemente no KDS, no cadastro de produto e nos relatórios"). `value` é a CHAVE semântica
 * persistida (`Station.Color`), nunca um hexadecimal literal (ADR-010: nenhum componente usa cor
 * literal) — a cor real vive só em `packages/ui/src/tokens/colors.css`; `STATION_COLOR_CSS_VAR`
 * abaixo é o único ponto que faz a ponte chave → `var(--nx-*)` para renderização.
 */
export const STATION_COLOR_OPTIONS = [
  { label: 'Navy', value: 'navy' },
  { label: 'Azul', value: 'blue' },
  { label: 'Ciano', value: 'cyan' },
  { label: 'Verde-água', value: 'teal' },
  { label: 'Verde', value: 'green' },
  { label: 'Âmbar', value: 'amber' },
  { label: 'Vermelho', value: 'red' },
  { label: 'Cinza', value: 'gray' },
] as const;

export type StationColorKey = (typeof STATION_COLOR_OPTIONS)[number]['value'];

/** Única ponte entre a chave semântica persistida e o token CSS real (variante `-500` da rampa de marca). */
export const STATION_COLOR_CSS_VAR: Record<StationColorKey, string> = {
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
  if (color && color in STATION_COLOR_CSS_VAR)
    return STATION_COLOR_CSS_VAR[color as StationColorKey];
  return 'var(--border-default)';
}

export class StationsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(): Promise<StationListResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/stations`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return stationListResponseSchema.parse(await response.json());
  }

  async create(input: CreateStationRequest): Promise<StationDto> {
    return this.write('/v1/catalog/stations', { method: 'POST', body: JSON.stringify(input) });
  }

  async update(id: string, input: UpdateStationRequest): Promise<StationDto> {
    return this.write(`/v1/catalog/stations/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  async remove(id: string): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/stations/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      },
    );
    await requireSuccess(response);
  }

  private async write(path: string, init: RequestInit): Promise<StationDto> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, {
      ...init,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
        ...init.headers,
      },
    });
    await requireSuccess(response);
    return stationSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
