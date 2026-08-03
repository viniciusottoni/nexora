import { describe, expect, it, vi } from 'vitest';
import { DevicesApi, DevicesApiError } from './devices-api.js';

describe('DevicesApi', () => {
  it('envia Idempotency-Key em toda escrita e não envia tenant pelo cliente', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }),
    );
    const api = new DevicesApi('/api', fetcher);

    await api.rename('device-id', 'Caixa 2');
    await api.revoke('device-id');
    await api.remove('device-id');

    for (const call of fetcher.mock.calls) {
      const options = call[1];
      expect(new Headers(options?.headers).get('Idempotency-Key')).toBeTruthy();
      expect(options?.body ?? '').not.toContain('tenant');
    }
  });

  it('exclui dispositivo via POST .../delete, sem reusar o DELETE já ocupado pela revogação', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }),
    );
    const api = new DevicesApi('/api', fetcher);

    await api.remove('device-id');

    expect(fetcher).toHaveBeenCalledWith(
      '/api/v1/devices/device-id/delete',
      expect.objectContaining({ method: 'POST' }),
    );
  });
  it('preserva status e codigo quando a sessao da API expira', async () => {
    const fetcher = vi.fn(
      async () =>
        new Response(
          JSON.stringify({
            code: 'AUTH_SESSION_IDLE_TIMEOUT',
            detail: 'Sess\u00e3o expirada por inatividade.',
          }),
          { status: 401 },
        ),
    );
    const api = new DevicesApi('/edge', fetcher);

    const error = await api.list().catch((reason: unknown) => reason);

    expect(error).toBeInstanceOf(DevicesApiError);
    expect(error).toMatchObject({ status: 401, code: 'AUTH_SESSION_IDLE_TIMEOUT' });
  });
});
