import { describe, expect, it } from 'vitest';

import {
  createTenantRequestSchema,
  createTenantResponseSchema,
  tenantDirectoryQuerySchema,
  tenantDirectoryResponseSchema,
  tenantHealthSchema,
  tenantListResponseSchema,
  tenantStatusSchema,
  tenantStatusTransitionResponseSchema,
} from './tenants.js';

describe('createTenantRequestSchema', () => {
  it('aceita o contrato normativo de provisionamento', () => {
    expect(
      createTenantRequestSchema.parse({
        name: 'Pizzaria Dona Betinha',
        slug: 'dona-betinha',
        plan: 'COMPLETO',
        template: 'PIZZERIA',
        owner: { name: 'Betinha', email: 'betinha@example.com' },
        store: { name: 'Matriz', timezone: 'America/Sao_Paulo' },
      }),
    ).toMatchObject({ slug: 'dona-betinha', template: 'PIZZERIA' });
  });

  it.each(['Dona-Betinha', '-dona-betinha', 'dona--betinha'])(
    'recusa slug invalido: %s',
    (slug) => {
      expect(() =>
        createTenantRequestSchema.parse({
          name: 'Dona Betinha',
          slug,
          plan: 'COMPLETO',
          template: 'PIZZERIA',
          owner: { name: 'Betinha', email: 'betinha@example.com' },
          store: { name: 'Matriz', timezone: 'America/Sao_Paulo' },
        }),
      ).toThrow();
    },
  );
});

describe('tenantListResponseSchema', () => {
  it('aceita o status normalizado em caixa alta (US-151 §7)', () => {
    expect(
      tenantListResponseSchema.parse({
        data: [
          {
            id: crypto.randomUUID(),
            slug: 'dona-betinha',
            name: 'Dona Betinha',
            plan: 'COMPLETO',
            status: 'ACTIVE',
            createdAt: '2026-08-05T12:00:00Z',
          },
        ],
      }),
    ).toMatchObject({ data: [{ status: 'ACTIVE' }] });
  });

  it('recusa status fora do enum do domínio', () => {
    expect(() =>
      tenantListResponseSchema.parse({
        data: [
          {
            id: crypto.randomUUID(),
            slug: 'dona-betinha',
            name: 'Dona Betinha',
            plan: 'COMPLETO',
            status: 'DRAFT',
            createdAt: '2026-08-05T12:00:00Z',
          },
        ],
      }),
    ).toThrow();
  });

  it('recusa o status legado em PascalCase — o fio agora é caixa alta', () => {
    expect(() => tenantStatusSchema.parse('Active')).toThrow();
  });
});

describe('tenantStatusSchema (US-153 · Ciclo de vida do estabelecimento)', () => {
  it.each(['PROVISIONED', 'INSTALLING', 'ACTIVE', 'SUSPENDED', 'CANCELLED'])(
    'aceita o valor canônico %s',
    (status) => {
      expect(tenantStatusSchema.parse(status)).toBe(status);
    },
  );

  it('recusa o valor legado TRIAL — substituído por PROVISIONED nesta história', () => {
    expect(() => tenantStatusSchema.parse('TRIAL')).toThrow();
  });
});

describe('tenantStatusTransitionResponseSchema', () => {
  it('aceita o contrato de POST /v1/platform/tenants/{id}/status-transitions (US-153 §7)', () => {
    expect(
      tenantStatusTransitionResponseSchema.parse({
        tenantId: crypto.randomUUID(),
        previousStatus: 'ACTIVE',
        status: 'SUSPENDED',
        version: 8,
        changedAt: '2026-08-05T12:00:00Z',
      }),
    ).toMatchObject({ previousStatus: 'ACTIVE', status: 'SUSPENDED', version: 8 });
  });

  it('recusa version não positiva (controle de concorrência otimista, ADR-020/ADR-023)', () => {
    expect(() =>
      tenantStatusTransitionResponseSchema.parse({
        tenantId: crypto.randomUUID(),
        previousStatus: 'ACTIVE',
        status: 'SUSPENDED',
        version: 0,
        changedAt: '2026-08-05T12:00:00Z',
      }),
    ).toThrow();
  });

  it('recusa status fora do enum canônico', () => {
    expect(() =>
      tenantStatusTransitionResponseSchema.parse({
        tenantId: crypto.randomUUID(),
        previousStatus: 'ACTIVE',
        status: 'TRIAL',
        version: 1,
        changedAt: '2026-08-05T12:00:00Z',
      }),
    ).toThrow();
  });
});

describe('tenantDirectoryResponseSchema', () => {
  const validItem = {
    id: crypto.randomUUID(),
    name: 'Dona Betinha',
    slug: 'dona-betinha',
    status: 'ACTIVE',
    plan: 'COMPLETO',
    ownerEmail: 'dono@example.com',
    storesCount: 1,
    installationsCount: 1,
    health: 'OK',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-08-01T00:00:00Z',
  };

  it('aceita o payload normativo do diretório (US-151 §7)', () => {
    expect(
      tenantDirectoryResponseSchema.parse({
        data: [validItem],
        nextCursor: null,
        appliedFilters: {
          query: 'betinha',
          status: ['ACTIVE'],
          plan: [],
          template: [],
          health: [],
          createdFrom: null,
          createdTo: null,
          sort: 'attention',
          limit: 25,
        },
      }),
    ).toMatchObject({ data: [{ status: 'ACTIVE', health: 'OK' }], nextCursor: null });
  });

  it('aceita ownerEmail nulo e timestamps com offset', () => {
    expect(
      tenantDirectoryResponseSchema.parse({
        data: [
          {
            ...validItem,
            ownerEmail: null,
            createdAt: '2026-01-01T00:00:00+00:00',
            updatedAt: '2026-08-01T00:00:00+00:00',
          },
        ],
        nextCursor: null,
        appliedFilters: {
          status: [],
          plan: [],
          template: [],
          health: [],
          sort: 'attention',
          limit: 25,
        },
      }).data[0]?.ownerEmail,
    ).toBeNull();
  });

  it('recusa status fora do enum caixa alta', () => {
    expect(() =>
      tenantDirectoryResponseSchema.parse({
        data: [{ ...validItem, status: 'Active' }],
        nextCursor: null,
        appliedFilters: {
          status: [],
          plan: [],
          template: [],
          health: [],
          sort: 'attention',
          limit: 25,
        },
      }),
    ).toThrow();
  });

  it('recusa saúde fora do enum OK|DEGRADED|DOWN|UNKNOWN', () => {
    expect(() => tenantHealthSchema.parse('CRITICAL')).toThrow();
    expect(() =>
      tenantDirectoryResponseSchema.parse({
        data: [{ ...validItem, health: 'CRITICAL' }],
        nextCursor: null,
        appliedFilters: {
          status: [],
          plan: [],
          template: [],
          health: [],
          sort: 'attention',
          limit: 25,
        },
      }),
    ).toThrow();
  });
});

describe('tenantDirectoryQuerySchema', () => {
  it('aceita todos os params vazios (defaults ficam por conta de quem monta a querystring)', () => {
    expect(tenantDirectoryQuerySchema.parse({})).toEqual({});
  });

  it('aceita filtros repetíveis e datas ISO', () => {
    expect(
      tenantDirectoryQuerySchema.parse({
        query: 'betinha',
        status: ['ACTIVE', 'SUSPENDED'],
        plan: ['COMPLETO'],
        template: ['PIZZERIA'],
        health: ['DEGRADED'],
        createdFrom: '2026-01-01T00:00:00Z',
        createdTo: '2026-12-31T23:59:59Z',
        sort: 'attention',
        limit: 25,
        cursor: 'opaque-cursor',
      }),
    ).toMatchObject({ status: ['ACTIVE', 'SUSPENDED'], sort: 'attention' });
  });

  it('recusa sort fora de name|createdAt|updatedAt|attention', () => {
    expect(() => tenantDirectoryQuerySchema.parse({ sort: 'popularity' })).toThrow();
  });
});

describe('createTenantResponseSchema', () => {
  it('nao aceita resposta sem os nove passos de implantacao', () => {
    expect(() =>
      createTenantResponseSchema.parse({
        tenant: { id: crypto.randomUUID(), slug: 'dona-betinha', status: 'PROVISIONED' },
        store: { id: crypto.randomUUID(), name: 'Matriz' },
        installToken: 'segredo',
        installCommand: './install.sh',
        ownerInviteSentTo: 'betinha@example.com',
        checklist: [],
      }),
    ).toThrow();
  });
});
