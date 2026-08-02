// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type {
  CategoryDto,
  ProductDto,
  VariantDto,
  VariantPriceTableResponse,
} from '@nexora/contracts';
import { PricingSection } from './pricing-section.js';
import type { PricingApi } from './pricing-api.js';
import type { VariantsApi } from '../catalog/variants-api.js';

describe('PricingSection', () => {
  it('monta a previa com o preco efetivo do canal escolhido', async () => {
    const category = { id: 'cat-1', name: 'Pizzas' } as CategoryDto;
    const product = { id: 'prod-1', categoryId: category.id, name: 'Mussarela' } as ProductDto;
    const variant = { id: 'var-1', productId: product.id, name: 'Grande' } as VariantDto;
    const table = {
      variantId: variant.id,
      productId: product.id,
      channels: [
        {
          channel: 'DineIn',
          amount: '45.00',
          isInherited: false,
          validFrom: '2026-01-01T00:00:00Z',
        },
        {
          channel: 'Delivery',
          amount: '52.00',
          isInherited: false,
          validFrom: '2026-01-01T00:00:00Z',
        },
        {
          channel: 'Takeout',
          amount: '45.00',
          isInherited: true,
          validFrom: '2026-01-01T00:00:00Z',
        },
        {
          channel: 'Marketplace',
          amount: '45.00',
          isInherited: true,
          validFrom: '2026-01-01T00:00:00Z',
        },
      ],
    } satisfies VariantPriceTableResponse;
    const variantsApi = {
      listForProduct: vi.fn().mockResolvedValue({ items: [variant] }),
    } as unknown as VariantsApi;
    const pricingApi = {
      getPriceTable: vi.fn().mockResolvedValue(table),
      setPriceTable: vi.fn(),
      bulkAdjust: vi.fn(),
    } as unknown as PricingApi;

    render(
      <PricingSection
        categories={[category]}
        products={[product]}
        pricingApi={pricingApi}
        variantsApi={variantsApi}
      />,
    );

    fireEvent.change(screen.getByLabelText('Produto'), { target: { value: product.id } });
    await screen.findByLabelText('Variação');
    await screen.findByLabelText('Preço do canal Delivery');
    fireEvent.click(screen.getByRole('button', { name: 'Pré-visualizar' }));

    await waitFor(() => expect(screen.getByText('Mussarela · Grande')).toBeInTheDocument());
    expect(screen.getAllByText('52,00')).toHaveLength(2);
  });
});
