// @vitest-environment jsdom
// authenticatedFetch (packages/ui/src/auth/cloud-auth.tsx) usa localStorage por padrão — só existe
// em ambiente jsdom (o padrão global deste monorepo é "node", ver vitest.config.ts).
import { describe, expect, it, vi } from 'vitest';
import { createSupportAccessApi } from './support-access-api.js';

const grantResponse = {
  token: 'abcd1234efgh5678ijkl',
  expiresAt: '2026-08-04T11:00:00Z',
  notifiedCustomer: true,
};

describe('createSupportAccessApi', () => {
  it('envia uma Idempotency-Key e posta no tenant informado', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(grantResponse), {
          status: 201,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createSupportAccessApi('/api');
    const result = await api.grant('tenant-123', { reason: 'Chamado #1', durationMinutes: 60 });

    expect(result).toMatchObject({ token: grantResponse.token });
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/platform/tenants/tenant-123/support-access');
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('idempotency-key')).toBeTruthy();

    vi.unstubAllGlobals();
  });

  it('propaga a mensagem de erro do problem details em falha', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Estabelecimento não encontrado.' }), {
          status: 404,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createSupportAccessApi('/api');

    await expect(
      api.grant('tenant-inexistente', { reason: 'Chamado #2', durationMinutes: 60 }),
    ).rejects.toThrow('Estabelecimento não encontrado.');

    vi.unstubAllGlobals();
  });
});
