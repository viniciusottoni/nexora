// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import type { KdsQueueItem } from '@nexora/contracts';
import { cleanup, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { AllDayPanel } from './all-day-panel.js';

afterEach(() => {
  cleanup();
});

let seq = 0;

function makeItem(overrides: Partial<KdsQueueItem> = {}): KdsQueueItem {
  seq += 1;
  return {
    orderItemId: `item-${seq}`,
    orderId: `order-${seq}`,
    orderCode: `A${seq}`,
    productId: `product-${seq}`,
    productName: 'Pizza G Mussarela',
    quantity: 1,
    modifiers: [],
    notes: null,
    status: 'QUEUED',
    placedAt: new Date().toISOString(),
    elapsedSeconds: 0,
    thresholdState: 'NORMAL',
    warnSeconds: 300,
    criticalSeconds: 600,
    table: null,
    channel: 'DineIn',
    fractions: [],
    ...overrides,
  };
}

describe('AllDayPanel (US-043)', () => {
  it('mostra "Sem pendências" e nenhuma lista quando a fila está vazia', () => {
    render(<AllDayPanel items={[]} />);

    expect(screen.getByTestId('all-day-panel-empty')).toHaveTextContent('Sem pendências');
    expect(screen.queryByTestId('all-day-panel-list')).not.toBeInTheDocument();
  });

  it('consolida por produto e exibe "Pizza G Mussarela · 12" (cenário de aceite)', () => {
    const items = Array.from({ length: 12 }, () => makeItem({ productName: 'Pizza G Mussarela', quantity: 1 }));

    render(<AllDayPanel items={items} />);

    const list = screen.getByTestId('all-day-panel-list');
    const [firstItem] = within(list).getAllByRole('listitem');
    expect(firstItem).toHaveTextContent('Pizza G Mussarela');
    expect(firstItem).toHaveTextContent('12');
  });

  it('conta fração proporcionalmente — 4 pedidos de meio a meio com metade de Mussarela mostram 2, não 4', () => {
    const items = Array.from({ length: 4 }, () =>
      makeItem({
        productName: 'Pizza G Meio a Meio',
        quantity: 1,
        fractions: [
          { productName: 'Mussarela', weight: '0.5' },
          { productName: 'Calabresa', weight: '0.5' },
        ],
      }),
    );

    render(<AllDayPanel items={items} />);

    const list = screen.getByTestId('all-day-panel-list');
    const rows = within(list).getAllByRole('listitem');
    const mussarelaRow = rows.find((row) => row.textContent?.includes('Mussarela'));
    expect(mussarelaRow).toHaveTextContent('2');
    expect(mussarelaRow).not.toHaveTextContent('4');
  });

  it('ordena a lista por quantidade pendente decrescente', () => {
    const items = [
      ...Array.from({ length: 3 }, () => makeItem({ productName: 'Pizza P Calabresa' })),
      ...Array.from({ length: 8 }, () => makeItem({ productName: 'Pizza G Mussarela' })),
      ...Array.from({ length: 5 }, () => makeItem({ productName: 'Pizza M Portuguesa' })),
    ];

    render(<AllDayPanel items={items} />);

    const list = screen.getByTestId('all-day-panel-list');
    const rows = within(list).getAllByRole('listitem').map((row) => row.textContent);
    expect(rows[0]).toContain('Pizza G Mussarela');
    expect(rows[1]).toContain('Pizza M Portuguesa');
    expect(rows[2]).toContain('Pizza P Calabresa');
  });

  it('atualiza em tempo real: reduzir de 12 para 11 basta re-renderizar com `items` menor', () => {
    const items = Array.from({ length: 12 }, () => makeItem({ productName: 'Pizza G Mussarela', quantity: 1 }));
    const { rerender } = render(<AllDayPanel items={items} />);
    expect(within(screen.getByTestId('all-day-panel-list')).getAllByRole('listitem')[0]).toHaveTextContent('12');

    // Um item avançou pra READY e a fila (que só traz status ativo) não o inclui mais.
    rerender(<AllDayPanel items={items.slice(1)} />);

    expect(within(screen.getByTestId('all-day-panel-list')).getAllByRole('listitem')[0]).toHaveTextContent('11');
  });
});
