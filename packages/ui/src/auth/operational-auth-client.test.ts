// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { OperationalAuthClient } from './operational-auth-client.js';

describe('OperationalAuthClient', () => {
  it('envia deviceId e mantém Idempotency-Key durante retries da mesma intenção', async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(new Response('{}', { status: 503 }))
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({ accessToken: 'token', user: { id: 'u', name: 'Ana' }, permissions: [] }),
          { status: 200, headers: { 'content-type': 'application/json' } },
        ),
      );
    const client = new OperationalAuthClient({
      baseUrl: '',
      deviceId: '0198a368-5ca3-7a0d-b231-183f77ae31a4',
      deviceSecret: 'secret',
      fetch,
      storage: localStorage,
      createKey: () => '0198a368-5ca3-7a0d-b231-183f77ae31a5',
    });
    await expect(client.loginWithPin('4821', 'troca-turno')).rejects.toThrow();
    await client.loginWithPin('4821', 'troca-turno');
    const first = (fetch.mock.calls[0]?.[1] as RequestInit).headers as Record<string, string>;
    const second = (fetch.mock.calls[1]?.[1] as RequestInit).headers as Record<string, string>;
    expect(first['Idempotency-Key']).toBe(second['Idempotency-Key']);
    expect(JSON.parse((fetch.mock.calls[0]?.[1] as RequestInit).body as string)).toMatchObject({
      deviceId: '0198a368-5ca3-7a0d-b231-183f77ae31a4',
    });
  });

  it('envia token pontual somente no header da ação sensível', async () => {
    const fetch = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ ok: true }), { status: 200 }),
    );
    const client = new OperationalAuthClient({
      baseUrl: '',
      deviceId: '0198a368-5ca3-7a0d-b231-183f77ae31a4',
      deviceSecret: 'secret',
      fetch,
      storage: localStorage,
      createKey: () => crypto.randomUUID(),
    });
    await client.executeSensitive(
      '/v1/orders/item/cancel',
      { reason: 'erro' },
      'cancelar-a47',
      'authorization-token',
    );
    const headers = (fetch.mock.calls[0]?.[1] as RequestInit).headers as Record<string, string>;
    expect(headers['X-Authorization-Token']).toBe('authorization-token');
  });
});
