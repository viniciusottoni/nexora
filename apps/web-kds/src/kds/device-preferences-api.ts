import {
  devicePreferencesPatchSchema,
  devicePreferencesResponseSchema,
  type DevicePreferencesResponse,
  type KdsDevicePreferences,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

/** Erro de API com o `code` estável do RFC 7807 (ADR-021) preservado — mesmo padrão de `KdsApiError` em `kds-queue-api.ts`. */
export class DevicePreferencesApiError extends Error {
  constructor(
    message: string,
    readonly code: string | undefined,
  ) {
    super(message);
    this.name = 'DevicePreferencesApiError';
  }
}

/**
 * Cliente HTTP de `PATCH /v1/devices/{id}/preferences` (US-042 `stationIds`/US-045 `sound`/US-047
 * `peakMode`, todas na mesma sub-chave `kds`) — mesmo padrão de `KdsQueueApi`
 * (`operationalAuthenticatedFetch`, parse Zod, `Idempotency-Key` por chamada). O servidor faz
 * mescla profunda (`DevicePreferencesJsonMerger`): só a sub-chave enviada muda, o resto das
 * preferências do dispositivo é preservado.
 */
export class DevicePreferencesApi {
  constructor(
    private readonly baseUrl = '',
    // Ver comentário em kds-queue-api.ts sobre por que não é `fetch.bind(globalThis)`.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  /**
   * Envia só a fatia `kds` que mudou (ex.: `{ sound: { volume: 0.6 } }`) — nunca o objeto de
   * preferências inteiro. CORREÇÃO (achado durante US-042): o corpo precisa ir dentro de
   * `{"preferences": ...}` — `UpdateDevicePreferencesRequest(JsonElement Preferences)`
   * (backend/src/Nexora.Contracts/Devices/UpdateDevicePreferencesRequest.cs) é um record com essa
   * ÚNICA propriedade; o binder padrão do ASP.NET Core (`System.Text.Json`) só a preenche se a
   * chave `preferences` existir no corpo — mandar `{"kds": {...}}` direto (sem o envelope) deixa
   * `Preferences` como `default(JsonElement)` (`Kind = Undefined`) e `GetRawText()` no handler
   * lança `InvalidOperationException` (500), nunca chegando a mesclar nada. O comentário anterior
   * deste arquivo (e o da própria classe `UpdateDevicePreferencesRequest.cs`) descrevia o corpo
   * como `{"kds": {...}}` solto — está desalinhado com o tipo real do parâmetro do record.
   */
  async updateKdsPreferences(
    identity: Readonly<OperationalRequestIdentity>,
    kdsPatch: Partial<KdsDevicePreferences>,
  ): Promise<DevicePreferencesResponse> {
    const preferences = devicePreferencesPatchSchema.parse({ kds: kdsPatch });
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/devices/${encodeURIComponent(identity.deviceId)}/preferences`,
      {
        method: 'PATCH',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify({ preferences }),
      },
      identity,
      this.fetcher,
    );
    await requireSuccess(response);
    return devicePreferencesResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as
    | { detail?: string; code?: string }
    | null;
  throw new DevicePreferencesApiError(
    problem?.detail ?? 'Não foi possível salvar as preferências deste dispositivo.',
    problem?.code,
  );
}
