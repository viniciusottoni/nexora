import { describe, expect, it } from 'vitest';
import { createVariantRequestSchema, variantSchema } from './catalog-variants.js';

const id = '0198aabb-4444-7000-8000-000000000001';

describe('contratos de variantes', () => {
  it('exige dinheiro como string com duas casas', () => {
    expect(
      createVariantRequestSchema.safeParse({ name: 'Grande', basePrice: '45.90' }).success,
    ).toBe(true);
    expect(createVariantRequestSchema.safeParse({ name: 'Grande', basePrice: 45.9 }).success).toBe(
      false,
    );
    expect(
      createVariantRequestSchema.safeParse({ name: 'Grande', basePrice: '45.999' }).success,
    ).toBe(false);
  });

  it('valida a resposta com preço e canal explícitos', () => {
    expect(
      variantSchema.safeParse({
        id,
        productId: id,
        name: 'Grande',
        sku: null,
        sizeCode: 'G',
        prepMinutes: 10,
        isDefault: true,
        isActive: true,
        currentPrice: '45.90',
        currentPriceChannel: 'DineIn',
      }).success,
    ).toBe(true);
  });
});
