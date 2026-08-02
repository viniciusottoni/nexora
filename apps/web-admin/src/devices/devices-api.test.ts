import { describe, expect, it, vi } from 'vitest';
import { DevicesApi } from './devices-api.js';

describe('DevicesApi', () => {
  it('envia Idempotency-Key em toda escrita e não envia tenant pelo cliente', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }),
    );
    const api = new DevicesApi('/api', fetcher);

    await api.rename('device-id', 'Caixa 2');
    await api.revoke('device-id');

    for (const call of fetcher.mock.calls) {
      const options = call[1];
      expect(new Headers(options?.headers).get('Idempotency-Key')).toBeTruthy();
      expect(options?.body ?? '').not.toContain('tenant');
    }
  });
});
