import { describe, expect, it } from 'vitest';
import {
  confirmProductImageRequestSchema,
  prepareProductImageUploadRequestSchema,
  publicMenuProductSchema,
} from './catalog-products.js';

const sha256 = 'a'.repeat(64);

describe('contratos de mídia de produto', () => {
  it('aceita HEIC até 10 MB', () => {
    expect(
      prepareProductImageUploadRequestSchema.safeParse({
        contentType: 'image/heic',
        bytes: 10_000_000,
        sha256,
      }).success,
    ).toBe(true);
  });

  it('recusa confirmação abaixo de 800x600', () => {
    expect(
      confirmProductImageRequestSchema.safeParse({
        url: 'https://cdn.example.com/photo.jpg',
        contentType: 'image/jpeg',
        bytes: 1_000,
        sha256,
        width: 799,
        height: 600,
      }).success,
    ).toBe(false);
  });

  it('exige preço público como string decimal ou null', () => {
    const base = {
      id: '0198aabb-1111-7000-8000-000000000001',
      name: 'Pizza',
      description: null,
      ingredientsText: null,
      allergens: [],
      imageUrl: null,
      position: 0,
    };

    expect(publicMenuProductSchema.parse({ ...base, fromPrice: '45.90' }).fromPrice).toBe('45.90');
    expect(() => publicMenuProductSchema.parse({ ...base, fromPrice: 45.9 })).toThrow();
  });
});
