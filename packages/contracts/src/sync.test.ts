import { describe, expect, it } from 'vitest';

import { initialSyncBootstrapPayloadSchema } from './sync.js';

const tenantId = '0198aabb-1111-7000-8000-000000000001';
const now = '2026-07-31T12:00:00.000Z';

function payload() {
  return {
    configVersion: 2,
    catalogVersion: 3,
    branding: {},
    operation: {},
    thresholds: {},
    modules: {},
    fiscal: {},
    printers: [],
    payments: {},
    maintenance: {},
    catalog: {
      stations: [],
      categories: [],
      products: [],
      variants: [],
      prices: [],
      modifierGroups: [],
      modifiers: [],
      productModifierGroups: [],
    },
    authorization: {
      roles: [],
      users: [
        {
          id: '0198aabb-1111-7000-8000-000000000002',
          tenantId,
          name: 'Operador',
          pinHash: '$argon2id$hash',
          pinLookup: 'lookup',
          status: 'ACTIVE',
          pinRotatedAt: null,
          createdAt: now,
          updatedAt: now,
          deletedAt: null,
        },
      ],
      userRoles: [],
    },
  };
}

describe('contrato da carga inicial', () => {
  it('aceita catálogo completo e credencial operacional mínima', () => {
    const result = initialSyncBootstrapPayloadSchema.parse(payload());

    expect(result.authorization.users[0]).toMatchObject({
      pinHash: '$argon2id$hash',
      pinLookup: 'lookup',
      status: 'ACTIVE',
    });
    expect(result.catalog).toMatchObject({
      modifierGroups: [],
      modifiers: [],
      productModifierGroups: [],
    });
  });

  it.each(['email', 'passwordHash', 'mfaSecret'])(
    'recusa campo sensível %s na réplica operacional',
    (field) => {
      const input = payload();
      Object.assign(input.authorization.users[0]!, { [field]: 'segredo' });

      expect(() => initialSyncBootstrapPayloadSchema.parse(input)).toThrow();
    },
  );
});
