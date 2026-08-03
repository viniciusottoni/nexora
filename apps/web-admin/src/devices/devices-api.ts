import {
  createPairingCodeResponseSchema,
  deviceListResponseSchema,
  type DeviceListResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export class DevicesApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string | undefined,
    message: string,
  ) {
    super(message);
  }
}

export class DevicesApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(): Promise<DeviceListResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/devices`, { credentials: 'include' });
    await requireSuccess(response);
    return deviceListResponseSchema.parse(await response.json());
  }

  async createPairingCode(): Promise<Readonly<{ code: string; expiresAt: string }>> {
    const response = await this.write('/v1/devices/pairing-codes', { method: 'POST' });
    const result = createPairingCodeResponseSchema.parse(await response.json());
    return { code: result.code, expiresAt: result.expiresAt };
  }

  async rename(id: string, label: string): Promise<void> {
    await this.write(`/v1/devices/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify({ label }),
    });
  }

  async revoke(id: string): Promise<void> {
    await this.write(`/v1/devices/${encodeURIComponent(id)}`, { method: 'DELETE' });
  }

  async remove(id: string): Promise<void> {
    await this.write(`/v1/devices/${encodeURIComponent(id)}/delete`, { method: 'POST' });
  }

  private async write(path: string, init: RequestInit): Promise<Response> {
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
    return response;
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as {
    code?: string;
    detail?: string;
  } | null;
  throw new DevicesApiError(
    response.status,
    problem?.code,
    problem?.detail ?? 'N\u00e3o foi poss\u00edvel concluir a opera\u00e7\u00e3o.',
  );
}
