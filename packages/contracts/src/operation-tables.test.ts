import { describe, expect, it } from 'vitest';
import {
  createTablesBulkRequestSchema,
  createTableRequestSchema,
  tableListResponseSchema,
  tableSchema,
} from './operation-tables.js';

const areaId = '0198aabb-1111-7000-8000-000000000001';
const tableId = '0198aabb-2222-7000-8000-000000000002';

describe('contratos de mesas do salao', () => {
  it('valida a criacao de uma mesa', () => {
    expect(createTableRequestSchema.parse({ areaId, label: '12', seats: 4 })).toEqual({
      areaId,
      label: '12',
      seats: 4,
    });
  });

  it('nunca expoe o qr_token no schema de mesa', () => {
    expect(Object.keys(tableSchema.shape)).not.toContain('qrToken');
  });

  it('aceita criacao em lote "1 a 20" e recusa intervalo invertido', () => {
    expect(createTablesBulkRequestSchema.parse({ areaId, from: 1, to: 20, seats: 4 })).toEqual({
      areaId,
      from: 1,
      to: 20,
      seats: 4,
    });

    expect(() => createTablesBulkRequestSchema.parse({ areaId, from: 20, to: 1, seats: 4 })).toThrow(
      'O número final deve ser maior ou igual ao inicial',
    );
  });

  it('recusa lote maior que o teto de seguranca', () => {
    expect(() =>
      createTablesBulkRequestSchema.parse({ areaId, from: 1, to: 500, seats: 4 }),
    ).toThrow('O lote não pode ter mais que 200 mesas de uma vez');
  });

  it('representa a lista de mesas com status canonico', () => {
    const response = tableListResponseSchema.parse({
      items: [
        {
          id: tableId,
          areaId,
          areaName: 'Salão',
          label: '12',
          seats: 4,
          status: 'FREE',
          active: true,
          sortOrder: 0,
        },
      ],
    });

    expect(response.items[0]?.status).toBe('FREE');
  });
});
