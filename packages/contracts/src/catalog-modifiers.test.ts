import { describe, expect, it } from 'vitest';
import {
  createModifierGroupRequestSchema,
  createModifierRequestSchema,
  updateModifierRequestSchema,
} from './catalog-modifiers.js';

describe('contratos de grupos de modificadores (US-012)', () => {
  it('recusa grupo obrigatório com mínimo zero', () => {
    expect(() =>
      createModifierGroupRequestSchema.parse({
        name: 'Tamanho',
        minSelect: 0,
        maxSelect: 1,
        isRequired: true,
        sortOrder: 0,
      }),
    ).toThrow('Grupo obrigatório precisa exigir ao menos uma seleção');
  });

  it('mantém dinheiro em duas casas e quantidade em até quatro casas', () => {
    expect(() => updateModifierRequestSchema.parse({ priceDelta: '1.001' })).toThrow(
      'Valor monetário inválido',
    );

    expect(
      createModifierRequestSchema.parse({
        name: 'Borda',
        priceDelta: '5.50',
        ingredientId: '0198aabb-1111-7000-8000-000000000001',
        quantity: '0.1234',
        sortOrder: 0,
      }),
    ).toMatchObject({ priceDelta: '5.50', quantity: '0.1234' });

    expect(() => updateModifierRequestSchema.parse({ priceDelta: 5.55 })).toThrow();
  });
});
