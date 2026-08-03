import { describe, expect, it } from 'vitest';
import { areaListResponseSchema, createAreaRequestSchema } from './operation-areas.js';

const areaId = '0198aabb-1111-7000-8000-000000000001';

describe('contratos de ambientes do salao', () => {
  it('aceita nome e posicao, com posicao padrao zero', () => {
    expect(createAreaRequestSchema.parse({ name: 'Salão' })).toEqual({ name: 'Salão', position: 0 });
    expect(createAreaRequestSchema.parse({ name: 'Varanda', position: 2 })).toEqual({
      name: 'Varanda',
      position: 2,
    });
  });

  it('recusa nome vazio', () => {
    expect(() => createAreaRequestSchema.parse({ name: '  ' })).toThrow();
  });

  it('representa a lista de ambientes com contagem de mesas', () => {
    const response = areaListResponseSchema.parse({
      items: [{ id: areaId, name: 'Salão', position: 1, active: true, tableCount: 20 }],
    });

    expect(response.items[0]?.tableCount).toBe(20);
  });
});
