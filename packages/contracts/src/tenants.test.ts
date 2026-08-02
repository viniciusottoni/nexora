import { describe, expect, it } from 'vitest';

import { createTenantRequestSchema, createTenantResponseSchema } from './tenants.js';

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
