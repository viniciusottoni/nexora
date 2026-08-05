// @vitest-environment jsdom
// authenticatedFetch (packages/ui/src/auth/cloud-auth.tsx) usa localStorage por padrão — só existe
// em ambiente jsdom (o padrão global deste monorepo é "node", ver vitest.config.ts).
import type { TenantOverviewResponse } from '@nexora/contracts';
import { describe, expect, it, vi } from 'vitest';
import { createTenantOverviewApi, isTenantStatusTransitionTarget } from './tenant-detail-api.js';

const overviewResponse: TenantOverviewResponse = {
  tenant: {
    id: '0198aabb-0001-7000-8000-000000000001',
    name: 'Pizzaria Dona Betinha',
    slug: 'dona-betinha',
    status: 'ACTIVE',
    statusVersion: 5,
    availableTransitions: ['SUSPENDED', 'CANCELLED'],
    plan: 'COMPLETO',
    template: 'PIZZERIA',
    domain: null,
    createdAt: '2026-01-10T12:00:00Z',
    updatedAt: '2026-02-01T09:00:00Z',
  },
  owner: { name: 'Betinha', email: 'betinha@example.com', inviteStatus: 'ACCEPTED' },
  stores: [{ id: '0198aabb-0002-7000-8000-000000000001', name: 'Matriz', timezone: 'America/Sao_Paulo' }],
  installations: [
    { id: '0198aabb-0003-7000-8000-000000000001', label: 'Matriz — edge', status: 'ACTIVE', health: 'OK' },
  ],
  deployment: { completed: 9, total: 9, nextAction: null },
  links: { publicMenu: null, admin: null, health: null },
};

describe('createTenantOverviewApi', () => {
  it('busca a visão 360 do tenant informado', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(overviewResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createTenantOverviewApi('/api');
    const result = await api.get(overviewResponse.tenant.id);

    expect(result).toMatchObject({ tenant: { name: 'Pizzaria Dona Betinha' } });
    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/platform/tenants/${overviewResponse.tenant.id}/overview`);
    expect(fetcher.mock.calls[0]?.[1]?.method).toBeUndefined();

    vi.unstubAllGlobals();
  });

  it('propaga status e mensagem do problem details em falha (ex.: 404)', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Estabelecimento não encontrado.', code: 'TENANT_NOT_FOUND' }), {
          status: 404,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createTenantOverviewApi('/api');

    await expect(api.get('tenant-inexistente')).rejects.toMatchObject({
      message: 'Estabelecimento não encontrado.',
      status: 404,
      code: 'TENANT_NOT_FOUND',
    });

    vi.unstubAllGlobals();
  });

  it('rejeita quando a resposta não bate com o schema (ex.: campo obrigatório ausente)', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ tenant: { id: 'not-a-uuid' } }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createTenantOverviewApi('/api');

    await expect(api.get('tenant-1')).rejects.toBeTruthy();

    vi.unstubAllGlobals();
  });
});

describe('isTenantStatusTransitionTarget', () => {
  it.each(['ACTIVE', 'SUSPENDED', 'CANCELLED'])('aceita %s como alvo de ação administrativa', (status) => {
    expect(isTenantStatusTransitionTarget(status)).toBe(true);
  });

  it.each(['PROVISIONED', 'INSTALLING'])(
    '%s não é um alvo manual — só alcançado pelo fluxo automático de provisionamento/instalação',
    (status) => {
      expect(isTenantStatusTransitionTarget(status)).toBe(false);
    },
  );
});

describe('createTenantOverviewApi().transitionStatus (US-153 · Ciclo de vida do estabelecimento)', () => {
  const transitionResponse = {
    tenantId: overviewResponse.tenant.id,
    previousStatus: 'ACTIVE',
    status: 'SUSPENDED',
    version: 6,
    changedAt: '2026-08-05T12:00:00Z',
  };

  it('envia Idempotency-Key e If-Match com o statusVersion informado (ADR-020/ADR-023)', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(transitionResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createTenantOverviewApi('/api');
    const result = await api.transitionStatus(overviewResponse.tenant.id, 5, {
      targetStatus: 'SUSPENDED',
      reason: 'Solicitação contratual #482',
    });

    expect(result).toMatchObject({ previousStatus: 'ACTIVE', status: 'SUSPENDED', version: 6 });
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe(`/api/v1/platform/tenants/${overviewResponse.tenant.id}/status-transitions`);
    expect(init.method).toBe('POST');
    const headers = init.headers as Record<string, string>;
    expect(headers['if-match']).toBe('"5"');
    expect(headers['idempotency-key']).toEqual(expect.any(String));
    expect(JSON.parse(init.body as string)).toEqual({
      targetStatus: 'SUSPENDED',
      reason: 'Solicitação contratual #482',
    });

    vi.unstubAllGlobals();
  });

  it('reaproveita a mesma Idempotency-Key ao repetir a mesma intenção (retry de rede, não nova transição)', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(transitionResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createTenantOverviewApi('/api');
    const input = { targetStatus: 'SUSPENDED' as const, reason: 'Solicitação contratual #482' };
    await api.transitionStatus(overviewResponse.tenant.id, 5, input);
    await api.transitionStatus(overviewResponse.tenant.id, 5, input);

    const firstKey = (fetcher.mock.calls[0]?.[1] as RequestInit).headers as Record<string, string>;
    const secondKey = (fetcher.mock.calls[1]?.[1] as RequestInit).headers as Record<string, string>;
    expect(firstKey['idempotency-key']).toBe(secondKey['idempotency-key']);

    vi.unstubAllGlobals();
  });

  it('propaga código e mensagem do problem details em falha (ex.: CONCURRENCY_CONFLICT)', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(
          JSON.stringify({ detail: 'Outro administrador já alterou este registro.', code: 'CONCURRENCY_CONFLICT' }),
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
    );
    vi.stubGlobal('fetch', fetcher);

    const api = createTenantOverviewApi('/api');

    await expect(
      api.transitionStatus(overviewResponse.tenant.id, 5, { targetStatus: 'SUSPENDED', reason: 'Motivo' }),
    ).rejects.toMatchObject({
      message: 'Outro administrador já alterou este registro.',
      status: 409,
      code: 'CONCURRENCY_CONFLICT',
    });

    vi.unstubAllGlobals();
  });
});
