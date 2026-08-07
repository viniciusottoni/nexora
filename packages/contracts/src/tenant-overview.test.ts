import { describe, expect, it } from 'vitest';

import { tenantOverviewResponseSchema } from './tenant-overview.js';

const baseTenant = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Pizzaria Dona Betinha',
  slug: 'dona-betinha',
  status: 'ACTIVE' as const,
  statusVersion: 3,
  availableTransitions: ['SUSPENDED', 'CANCELLED'] as const,
  plan: 'COMPLETO',
  template: 'PIZZERIA',
  domain: null,
  createdAt: '2026-08-01T12:00:00+00:00',
  updatedAt: '2026-08-05T12:00:00+00:00',
};

describe('tenantOverviewResponseSchema', () => {
  it('aceita o contrato de GET /v1/platform/tenants/{id}/overview com cadastro saudável', () => {
    expect(
      tenantOverviewResponseSchema.parse({
        tenant: baseTenant,
        owner: { name: 'Betina Souza', email: 'betina@example.com', inviteStatus: 'ACCEPTED' },
        stores: [
          {
            id: '22222222-2222-2222-2222-222222222222',
            name: 'Matriz',
            timezone: 'America/Sao_Paulo',
          },
        ],
        installations: [
          {
            id: '33333333-3333-3333-3333-333333333333',
            label: 'Servidor local — Matriz',
            status: 'ACTIVE',
            health: 'OK',
          },
        ],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: 'https://dona-betinha.plataforma.com.br', admin: null, health: null },
      }).deployment,
    ).toMatchObject({ completed: 9, total: 9, nextAction: null });
  });

  it('aceita provisionamento incompleto — instalação pendente e próxima ação preenchida', () => {
    const parsed = tenantOverviewResponseSchema.parse({
      tenant: baseTenant,
      owner: { name: 'Betina Souza', email: 'betina@example.com', inviteStatus: 'PENDING' },
      stores: [
        {
          id: '22222222-2222-2222-2222-222222222222',
          name: 'Matriz',
          timezone: 'America/Sao_Paulo',
        },
      ],
      installations: [
        {
          id: '33333333-3333-3333-3333-333333333333',
          label: 'Servidor local — Matriz',
          status: 'PENDING',
          health: 'UNKNOWN',
        },
      ],
      deployment: { completed: 4, total: 9, nextAction: 'EDGE_INSTALL' },
      links: { publicMenu: null, admin: null, health: null },
    });
    expect(parsed.deployment.nextAction).toBe('EDGE_INSTALL');
    expect(parsed.installations[0]?.status).toBe('PENDING');
  });

  it('aceita dono ausente (owner nulo) sem quebrar o restante do agregado', () => {
    expect(() =>
      tenantOverviewResponseSchema.parse({
        tenant: baseTenant,
        owner: null,
        stores: [],
        installations: [],
        deployment: { completed: 1, total: 9, nextAction: 'BRANDING' },
        links: { publicMenu: null, admin: null, health: null },
      }),
    ).not.toThrow();
  });

  it('recusa status de instalação fora do enum', () => {
    expect(() =>
      tenantOverviewResponseSchema.parse({
        tenant: baseTenant,
        owner: null,
        stores: [],
        installations: [
          {
            id: '33333333-3333-3333-3333-333333333333',
            label: 'x',
            status: 'UNKNOWN',
            health: 'OK',
          },
        ],
        deployment: { completed: 0, total: 9, nextAction: 'BRANDING' },
        links: { publicMenu: null, admin: null, health: null },
      }),
    ).toThrow();
  });

  // US-153 · Ciclo de vida do estabelecimento — `statusVersion` (concorrência otimista) e
  // `availableTransitions` (máquina de estados computada no servidor) passam a viajar em
  // `tenant.overview`; a UI nunca decide sozinha quais transições são legais.
  it('aceita statusVersion e availableTransitions (US-153 §7/§10)', () => {
    const parsed = tenantOverviewResponseSchema.parse({
      tenant: baseTenant,
      owner: null,
      stores: [],
      installations: [],
      deployment: { completed: 9, total: 9, nextAction: null },
      links: { publicMenu: null, admin: null, health: null },
    });
    expect(parsed.tenant.statusVersion).toBe(3);
    expect(parsed.tenant.availableTransitions).toEqual(['SUSPENDED', 'CANCELLED']);
  });

  it('recusa availableTransitions sem statusVersion e vice-versa (ambos obrigatórios)', () => {
    const { statusVersion: _statusVersion, ...tenantWithoutVersion } = baseTenant;
    expect(() =>
      tenantOverviewResponseSchema.parse({
        tenant: tenantWithoutVersion,
        owner: null,
        stores: [],
        installations: [],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: null, admin: null, health: null },
      }),
    ).toThrow();

    const { availableTransitions: _availableTransitions, ...tenantWithoutTransitions } = baseTenant;
    expect(() =>
      tenantOverviewResponseSchema.parse({
        tenant: tenantWithoutTransitions,
        owner: null,
        stores: [],
        installations: [],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: null, admin: null, health: null },
      }),
    ).toThrow();
  });

  it('recusa statusVersion não positivo (controle de concorrência otimista)', () => {
    expect(() =>
      tenantOverviewResponseSchema.parse({
        tenant: { ...baseTenant, statusVersion: 0 },
        owner: null,
        stores: [],
        installations: [],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: null, admin: null, health: null },
      }),
    ).toThrow();
  });

  it('recusa entrada de availableTransitions fora do enum canônico', () => {
    expect(() =>
      tenantOverviewResponseSchema.parse({
        tenant: { ...baseTenant, availableTransitions: ['TRIAL'] },
        owner: null,
        stores: [],
        installations: [],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: null, admin: null, health: null },
      }),
    ).toThrow();
  });
});
