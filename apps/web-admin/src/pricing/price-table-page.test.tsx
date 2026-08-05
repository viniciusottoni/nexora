// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { VariantChannelPriceRow } from '@nexora/contracts';
import { PriceTablePage } from './price-table-page.js';
import type { CategoryPriceSnapshotItem } from './price-table-page.js';

const variantId = '0198aabb-5555-7000-8000-000000000001';

const channelsWithDelivery: readonly VariantChannelPriceRow[] = [
  { channel: 'DineIn', amount: '45.00', isInherited: false, validFrom: '2026-01-01T00:00:00Z' },
  { channel: 'Delivery', amount: '52.00', isInherited: false, validFrom: '2026-01-01T00:00:00Z' },
  { channel: 'Takeout', amount: '45.00', isInherited: true, validFrom: '2026-01-01T00:00:00Z' },
  { channel: 'Marketplace', amount: '45.00', isInherited: true, validFrom: '2026-01-01T00:00:00Z' },
];

const channelsWithCheapDelivery: readonly VariantChannelPriceRow[] = [
  { channel: 'DineIn', amount: '45.00', isInherited: false, validFrom: '2026-01-01T00:00:00Z' },
  { channel: 'Delivery', amount: '30.00', isInherited: false, validFrom: '2026-01-01T00:00:00Z' },
  { channel: 'Takeout', amount: null, isInherited: false, validFrom: null },
  { channel: 'Marketplace', amount: null, isInherited: false, validFrom: null },
];

const categories = [{ id: 'cat-1', name: 'Pizzas Salgadas' }];

describe('PriceTablePage', () => {
  it('exibe os quatro canais com o preco vigente e marca herança do salão', () => {
    render(
      <PriceTablePage
        variantId={variantId}
        variantName="Pizza Mussarela — Grande"
        channels={channelsWithDelivery}
        categories={categories}
        onSaveChannelPrices={vi.fn()}
        onLoadCategoryPriceSnapshot={vi.fn()}
        onBulkAdjust={vi.fn()}
      />,
    );

    expect(screen.getByLabelText('Preço do canal Salão')).toHaveValue('45,00');
    expect(screen.getByLabelText('Preço do canal Delivery')).toHaveValue('52,00');
    expect(screen.getByLabelText('Preço do canal Balcão')).toHaveValue('45,00');
    expect(screen.getAllByText('Herdado do salão')).toHaveLength(2);
    expect(screen.getAllByText('Próprio')).toHaveLength(2);
  });

  it('avisa quando o preco de delivery e menor que o do salao (US-014 §10)', () => {
    render(
      <PriceTablePage
        variantId={variantId}
        variantName="Pizza Mussarela — Grande"
        channels={channelsWithCheapDelivery}
        categories={categories}
        onSaveChannelPrices={vi.fn()}
        onLoadCategoryPriceSnapshot={vi.fn()}
        onBulkAdjust={vi.fn()}
      />,
    );

    expect(screen.getByText('Delivery mais barato que o salão')).toBeInTheDocument();
  });

  it('so salva os canais efetivamente alterados', async () => {
    const onSave = vi
      .fn()
      .mockResolvedValue({ variantId, productId: 'prod-1', channels: channelsWithDelivery });
    render(
      <PriceTablePage
        variantId={variantId}
        variantName="Pizza Mussarela — Grande"
        channels={channelsWithDelivery}
        categories={categories}
        onSaveChannelPrices={onSave}
        onLoadCategoryPriceSnapshot={vi.fn()}
        onBulkAdjust={vi.fn()}
      />,
    );

    expect(screen.getAllByRole('button', { name: 'Salvar preços' }).at(-1)).toBeDisabled();

    fireEvent.change(screen.getAllByLabelText('Preço do canal Delivery').at(-1)!, {
      target: { value: '5500' },
    });
    expect(screen.getAllByRole('button', { name: 'Salvar preços' }).at(-1)).toBeEnabled();

    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar preços' }).at(-1)!);

    await waitFor(() =>
      expect(onSave).toHaveBeenCalledWith(variantId, {
        prices: [{ channel: 'Delivery', amount: '55.00' }],
      }),
    );
  });

  it('usa a resposta salva como nova base e atualiza a origem do canal', async () => {
    const savedChannels: readonly VariantChannelPriceRow[] = channelsWithDelivery.map((row) =>
      row.channel === 'Takeout' ? { ...row, amount: '55.00', isInherited: false } : row,
    );
    const onSave = vi
      .fn()
      .mockResolvedValue({ variantId, productId: 'prod-1', channels: savedChannels });
    render(
      <PriceTablePage
        variantId={variantId}
        variantName="Pizza Mussarela — Grande"
        channels={channelsWithDelivery}
        categories={categories}
        onSaveChannelPrices={onSave}
        onLoadCategoryPriceSnapshot={vi.fn()}
        onBulkAdjust={vi.fn()}
      />,
    );

    fireEvent.change(screen.getAllByLabelText('Preço do canal Balcão').at(-1)!, {
      target: { value: '5500' },
    });
    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar preços' }).at(-1)!);

    await waitFor(() =>
      expect(screen.getAllByText('Nenhuma alteração pendente').at(-1)).toBeInTheDocument(),
    );
    expect(screen.getAllByRole('button', { name: 'Salvar preços' }).at(-1)).toBeDisabled();
    const takeoutInput = screen.getAllByLabelText('Preço do canal Balcão').at(-1);
    expect(takeoutInput).toBeTruthy();
    expect(within(takeoutInput!.closest('tr')!).getByText('Próprio')).toBeInTheDocument();
  });

  it('pre-visualiza o reajuste em massa antes de confirmar', async () => {
    const snapshot: readonly CategoryPriceSnapshotItem[] = [
      { variantId: 'v1', variantName: 'Pizza Mussarela — Pequena', currentAmount: '35.00' },
      { variantId: 'v2', variantName: 'Pizza Mussarela — Grande', currentAmount: '52.00' },
    ];
    const onLoadSnapshot = vi.fn().mockResolvedValue(snapshot);
    const onBulkAdjust = vi
      .fn()
      .mockResolvedValue({ updated: 2, effectiveFrom: '2026-08-02T12:00:00Z' });

    render(
      <PriceTablePage
        variantId={variantId}
        variantName="Pizza Mussarela — Grande"
        channels={channelsWithDelivery}
        categories={categories}
        onSaveChannelPrices={vi.fn()}
        onLoadCategoryPriceSnapshot={onLoadSnapshot}
        onBulkAdjust={onBulkAdjust}
      />,
    );

    fireEvent.change(screen.getAllByLabelText('Percentual').at(-1)!, { target: { value: '8' } });
    fireEvent.click(screen.getAllByRole('button', { name: 'Pré-visualizar' }).at(-1)!);

    await waitFor(() => expect(onLoadSnapshot).toHaveBeenCalledWith('cat-1', 'Delivery'));
    expect(await screen.findByText('Pizza Mussarela — Pequena')).toBeInTheDocument();
    expect(screen.getByText('37,80')).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: 'Confirmar reajuste' }).at(-1)!);

    await waitFor(() =>
      expect(onBulkAdjust).toHaveBeenCalledWith({
        categoryId: 'cat-1',
        channel: 'Delivery',
        percent: 8,
      }),
    );
    expect(await screen.findByText(/2 preço\(s\) atualizado\(s\)/)).toBeInTheDocument();
  });
});
