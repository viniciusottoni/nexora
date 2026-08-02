import { describe, expect, it } from 'vitest';
import {
  categoryListResponseSchema,
  reorderCategoriesRequestSchema,
} from './catalog-categories.js';

describe('contratos de categorias', () => {
  it('valida listagem com contagem de produtos', () => {
    expect(
      categoryListResponseSchema.safeParse({
        items: [
          {
            id: '0198aabb-3333-7000-8000-000000000001',
            name: 'Pizzas',
            description: null,
            position: 0,
            isActive: true,
            productCount: 2,
          },
        ],
      }).success,
    ).toBe(true);
  });

  it('recusa reordenação vazia', () => {
    expect(reorderCategoriesRequestSchema.safeParse({ order: [] }).success).toBe(false);
  });
});
