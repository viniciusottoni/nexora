import { describe, expect, it, vi } from 'vitest';
import { AuditApi } from './audit-api.js';

const validEntry = {
  id: '0198aabb-1111-7000-8000-000000000001',
  action: 'DISCOUNT_APPLIED',
  summary: 'Desconto de 10% (R$ 19,80) aplicado',
  actor: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Carlos' },
  authorizedBy: { id: '0198aabb-1111-7000-8000-000000000003', name: 'Ana' },
  device: { id: '0198aabb-1111-7000-8000-000000000004', label: 'Caixa 1' },
  occurredAt: '2026-08-01T20:00:00Z',
  target: { type: 'order', id: '0198aabb-1111-7000-8000-000000000005', label: 'Pedido A47' },
  before: { discount: 0 },
  after: { discount: 1980 },
  reason: 'cortesia',
  traceId: '0123456789abcdef0123456789abcdef',
};

function json(value: unknown, status = 200): Response {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

describe('AuditApi', () => {
  it('monta a querystring só com os filtros informados e valida a resposta pelo schema', async () => {
    const fetcher = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) =>
      json({ data: [validEntry], meta: { nextCursor: 'cursor-2', hasMore: true } }),
    );
    const api = new AuditApi('/api', fetcher);

    const result = await api.list({
      from: '2026-08-01T00:00:00Z',
      to: '2026-08-08T00:00:00Z',
      actorId: '0198aabb-1111-7000-8000-000000000002',
      action: 'DISCOUNT_APPLIED',
      minAmount: '50.00',
      limit: 50,
    });

    expect(fetcher).toHaveBeenCalledTimes(1);
    const [url, init] = fetcher.mock.calls[0]!;
    expect(requestUrl(url)).toBe(
      '/api/v1/audit?from=2026-08-01T00%3A00%3A00Z&to=2026-08-08T00%3A00%3A00Z&actorId=0198aabb-1111-7000-8000-000000000002&action=DISCOUNT_APPLIED&minAmount=50.00&limit=50',
    );
    expect(init).toMatchObject({ credentials: 'include' });
    expect(result.data).toHaveLength(1);
    expect(result.data[0]?.summary).toBe('Desconto de 10% (R$ 19,80) aplicado');
    expect(result.meta).toEqual({ nextCursor: 'cursor-2', hasMore: true });
  });

  it('omite chaves vazias e chama sem querystring quando não há filtro', async () => {
    const fetcher = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) =>
      json({ data: [], meta: { nextCursor: null, hasMore: false } }),
    );
    const api = new AuditApi('/api', fetcher);

    await api.list({ action: '' });

    expect(fetcher).toHaveBeenCalledWith('/api/v1/audit', { credentials: 'include' });
  });

  it('usa mensagem de fallback quando a falha não traz problem details', async () => {
    const api = new AuditApi('/api', async () => new Response(null, { status: 403 }));

    await expect(api.list()).rejects.toThrow('Não foi possível consultar a trilha de auditoria.');
  });

  it('propaga o detail do problem details quando presente (ex. 403 sem permissão audit:read)', async () => {
    const api = new AuditApi(
      '/api',
      async () => json({ detail: 'Você não tem permissão para consultar a auditoria.' }, 403),
    );

    await expect(api.list()).rejects.toThrow('Você não tem permissão para consultar a auditoria.');
  });
});
