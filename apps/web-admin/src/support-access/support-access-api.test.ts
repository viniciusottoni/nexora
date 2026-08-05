import { describe, expect, it, vi } from 'vitest';
import { SupportAccessApiClient } from './support-access-api.js';

const grant = {
  id: '0198aabb-4444-7000-8000-000000000001',
  tenantId: '0198aabb-4444-7000-8000-00000000000a',
  tenantName: null,
  grantedTo: null,
  reason: 'Chamado #482',
  durationMinutes: 60,
  grantedAt: '2026-08-04T10:00:00Z',
  expiresAt: '2026-08-04T11:00:00Z',
  revokedAt: null,
  revokedBy: null,
  lastUsedAt: null,
  isActive: true,
};

describe('SupportAccessApiClient', () => {
  it('valida a resposta do historico', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ data: [grant] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new SupportAccessApiClient('/api', fetcher);

    await expect(api.history()).resolves.toMatchObject({ data: [{ reason: 'Chamado #482' }] });
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/tenant/support-access-history');
  });

  it('revoke chama DELETE com Idempotency-Key no id informado', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }),
    );
    const api = new SupportAccessApiClient('/api', fetcher);

    await api.revoke(grant.id);

    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/tenant/support-access/${grant.id}`);
    expect(fetcher.mock.calls[0]?.[1]?.method).toBe('DELETE');
    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('Idempotency-Key')).toBeTruthy();
  });

  it('propaga a mensagem de erro do problem details em falha', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Acesso de suporte não encontrado.' }), {
          status: 404,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new SupportAccessApiClient('/api', fetcher);

    await expect(api.revoke(grant.id)).rejects.toThrow('Acesso de suporte não encontrado.');
  });
});
