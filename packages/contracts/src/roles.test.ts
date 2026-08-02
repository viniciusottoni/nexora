import { describe, expect, it } from 'vitest';
import {
  createRoleRequestSchema,
  roleListResponseSchema,
  updateRoleRequestSchema,
} from './roles.js';

const roleId = '0198aabb-1111-7000-8000-000000000001';

describe('contratos de papeis e permissoes', () => {
  it('cria papel sem permissoes por padrao', () => {
    expect(createRoleRequestSchema.parse({ code: 'ATENDENTE', name: 'Atendente' })).toEqual({
      code: 'ATENDENTE',
      name: 'Atendente',
      permissions: [],
    });
  });

  it('aceita somente permissoes existentes no catalogo do produto', () => {
    expect(
      updateRoleRequestSchema.parse({
        name: 'Gerente de salao',
        permissions: ['table:open', 'order:cancel_started', 'table:open'],
      }),
    ).toEqual({
      name: 'Gerente de salao',
      permissions: ['table:open', 'order:cancel_started'],
    });

    expect(() =>
      updateRoleRequestSchema.parse({ permissions: ['tenant-specific:permission'] }),
    ).toThrow('Permissão desconhecida');

    expect(
      updateRoleRequestSchema.parse({
        permissions: ['supplier:*', 'delivery:read_own', 'delivery:advance'],
      }).permissions,
    ).toEqual(['supplier:*', 'delivery:read_own', 'delivery:advance']);
  });

  it('representa efeito pratico e risco de cada permissao', () => {
    const response = roleListResponseSchema.parse({
      items: [
        {
          id: roleId,
          code: 'ATENDENTE',
          name: 'Atendente',
          permissions: [],
          system: false,
          userCount: 0,
        },
      ],
      permissionCatalog: [
        {
          code: 'order:cancel_started',
          resource: 'Pedidos',
          description: 'Cancelar item que ja entrou em producao',
          sensitive: true,
        },
      ],
    });

    expect(response.items[0]?.permissions).toEqual([]);
    expect(response.permissionCatalog[0]?.sensitive).toBe(true);
  });
});
