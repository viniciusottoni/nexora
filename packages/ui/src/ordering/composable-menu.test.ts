import { describe, expect, it } from 'vitest';
import { composableMenuResponseSchema, mapComposableMenuResponseToProducts } from './composable-menu.js';

const tenantId = '0198aabb-1111-7000-8000-000000000001';
const categoryId = '0198aabb-2222-7000-8000-000000000002';
const productId = '0198aabb-3333-7000-8000-000000000003';

function baseCategory(products: readonly unknown[]) {
  return {
    tenantId,
    tenantName: 'Pizzaria Dona Betinha',
    categories: [
      {
        id: categoryId,
        name: 'Pizzas',
        description: null,
        position: 0,
        products,
      },
    ],
  };
}

describe('composable-menu (US-030 §10)', () => {
  it('degrada para 1 variante = o proprio produto quando o cardapio real nao traz variantes/modificadores', () => {
    const menu = composableMenuResponseSchema.parse(
      baseCategory([
        {
          id: productId,
          name: 'Pizza Mussarela',
          description: 'Molho, mussarela, orégano',
          ingredientsText: null,
          allergens: [],
          imageUrl: null,
          position: 0,
          fromPrice: '45.90',
        },
      ]),
    );

    const [product] = mapComposableMenuResponseToProducts(menu);
    expect(product?.variants).toEqual([{ id: productId, name: 'Pizza Mussarela', price: '45.90' }]);
    expect(product?.modifierGroups).toEqual([]);
    expect(product?.allowsFractions).toBe(false);
    expect(product?.fractionFlavors).toEqual([]);
    expect(product?.categoryName).toBe('Pizzas');
  });

  it('le variantes/grupos de modificador/sabores de fracao quando o cardapio ja os traz (extensao aditiva)', () => {
    const groupId = '0198aabb-4444-7000-8000-000000000004';
    const modifierId = '0198aabb-5555-7000-8000-000000000005';
    const variantId = '0198aabb-6666-7000-8000-000000000006';
    const flavorId = '0198aabb-7777-7000-8000-000000000007';

    const menu = composableMenuResponseSchema.parse(
      baseCategory([
        {
          id: productId,
          name: 'Pizza Grande',
          description: null,
          ingredientsText: null,
          allergens: [],
          imageUrl: null,
          position: 0,
          fromPrice: '52.00',
          variants: [{ id: variantId, name: 'Pizza Grande', price: '52.00' }],
          modifierGroups: [
            {
              id: groupId,
              name: 'Borda',
              minSelect: 1,
              maxSelect: 1,
              isRequired: true,
              modifiers: [{ id: modifierId, name: 'Catupiry', priceDelta: '8.00' }],
            },
          ],
          allowsFractions: true,
          maxFractions: 2,
          fractionFlavors: [
            {
              variantId: flavorId,
              name: 'Calabresa',
              fractionGroup: 'salgadas',
              price: '48.00',
              available: true,
            },
          ],
        },
      ]),
    );

    const [product] = mapComposableMenuResponseToProducts(menu);
    expect(product?.variants).toEqual([{ id: variantId, name: 'Pizza Grande', price: '52.00' }]);
    expect(product?.modifierGroups).toHaveLength(1);
    expect(product?.modifierGroups[0]?.isRequired).toBe(true);
    expect(product?.allowsFractions).toBe(true);
    expect(product?.fractionFlavors).toHaveLength(1);
    expect(product?.fractionFlavors[0]?.name).toBe('Calabresa');
  });
});
