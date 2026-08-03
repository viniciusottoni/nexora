// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ProductDto } from '@nexora/contracts';
import type { VariantsApi } from '../catalog/variants-api.js';
import type { PrepTimeApi } from './prep-time-api.js';
import { PrepTimeSection } from './prep-time-section.js';

const product: ProductDto = {
  id: '0198aabb-1111-7000-8000-000000000001',
  categoryId: '0198aabb-1111-7000-8000-000000000002',
  categoryName: 'Pizzas',
  stationId: null,
  stationName: null,
  name: 'Pizza Mussarela',
  description: null,
  ingredientsText: null,
  allergens: [],
  imageUrl: null,
  position: 0,
  isActive: true,
  isAvailable: true,
  allowsFractions: false,
  maxFractions: 1,
};

describe('PrepTimeSection', () => {
  it('exibe a variação carregada e tolera análise histórica indisponível', async () => {
    const variantsApi = {
      listForProduct: vi.fn(async () => ({
        items: [
          {
            id: '0198aabb-1111-7000-8000-000000000003',
            productId: product.id,
            name: 'Grande',
            sku: null,
            sizeCode: 'G',
            prepMinutes: 12,
            isDefault: true,
            isActive: true,
            currentPrice: '49.90',
            currentPriceChannel: 'DineIn' as const,
          },
        ],
      })),
    } as unknown as VariantsApi;
    const prepTimeApi = {
      getPrepTimeAnalysis: vi.fn(async () => {
        throw new Error('Sem histórico');
      }),
    } as unknown as PrepTimeApi;

    render(
      <PrepTimeSection
        products={[product]}
        stations={[]}
        prepTimeApi={prepTimeApi}
        variantsApi={variantsApi}
      />,
    );

    expect(await screen.findByText('Grande')).toBeInTheDocument();
    expect(screen.getByDisplayValue('12')).toBeInTheDocument();
  });

  it('mostra erro acionável quando não consegue carregar as variações', async () => {
    const variantsApi = {
      listForProduct: vi.fn(async () => {
        throw new Error('Catálogo indisponível.');
      }),
    } as unknown as VariantsApi;
    const prepTimeApi = {} as PrepTimeApi;

    render(
      <PrepTimeSection
        products={[product]}
        stations={[]}
        prepTimeApi={prepTimeApi}
        variantsApi={variantsApi}
      />,
    );

    expect(await screen.findByText('Catálogo indisponível.')).toBeInTheDocument();
  });
});
