import { describe, expect, it, vi } from 'vitest';
import { RolesApi } from './roles-api.js';

const role = {
  id: '0198aabb-1111-7000-8000-000000000001',
  code: 'ATENDENTE',
  name: 'Atendente',
  permissions: [],
  system: false,
  userCount: 0,
};

describe('RolesApi', () => {
  it('valida resposta da lista e nao envia tenant do navegador', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ items: [role], permissionCatalog: [] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new RolesApi('/api', fetcher);

    await expect(api.list()).resolves.toMatchObject({ items: [{ code: 'ATENDENTE' }] });
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/roles');
    expect(fetcher.mock.calls[0]?.[1]?.body ?? '').not.toContain('tenant');
  });

  it('envia uma Idempotency-Key nova em cada intencao de escrita', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(role), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new RolesApi('/api', fetcher);

    await api.create({ code: 'ATENDENTE', name: 'Atendente', permissions: [] });
    await api.update(role.id, { permissions: ['table:read'] });

    const keys = fetcher.mock.calls.map((call) =>
      new Headers(call[1]?.headers).get('Idempotency-Key'),
    );
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBeTruthy();
    expect(keys[0]).not.toBe(keys[1]);
    expect(fetcher.mock.calls.map((call) => call[1]?.body)).not.toContainEqual(
      expect.stringContaining('tenant'),
    );
  });
});
