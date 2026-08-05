// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, render, waitFor, fireEvent, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type {
  AvailabilityApi,
  AvailabilitySubscription,
  ProductAvailabilityChangedEvent,
} from './availability-api.js';
import { UnavailableListPage } from './unavailable-list-page.js';

function noopSubscribe(): AvailabilitySubscription {
  return { close: () => undefined };
}

describe('UnavailableListPage', () => {
  it('carrega e exibe os itens indisponíveis do tenant', async () => {
    const listUnavailable = vi.fn(async () => ({
      items: [
        {
          productId: 'p1',
          productName: 'Pizza Calabresa',
          isAvailable: false,
          unavailableReason: 'Acabou o insumo',
          unavailableSince: '2026-08-02T20:00:00.000Z',
        },
      ],
    }));
    const api = { listUnavailable } as unknown as AvailabilityApi;

    const { container } = render(<UnavailableListPage api={api} subscribeFn={noopSubscribe} />);
    const page = within(container);

    expect(await page.findByText('Pizza Calabresa')).toBeInTheDocument();
    expect(page.getByText('Acabou o insumo')).toBeInTheDocument();
    expect(page.getByText('1')).toBeInTheDocument();
  });

  it('lista vazia mostra estado neutro, nunca uma tabela vazia sem contexto', async () => {
    const api = {
      listUnavailable: vi.fn(async () => ({ items: [] })),
    } as unknown as AvailabilityApi;

    const { container } = render(<UnavailableListPage api={api} subscribeFn={noopSubscribe} />);
    const page = within(container);

    expect(await page.findByText('Nenhum item indisponível')).toBeInTheDocument();
  });

  it('marcar disponível remove o item da lista', async () => {
    const markAvailable = vi.fn(async () => ({
      productId: 'p1',
      productName: 'Pizza Calabresa',
      isAvailable: true,
      unavailableReason: null,
      unavailableSince: null,
    }));
    const api = {
      listUnavailable: vi.fn(async () => ({
        items: [
          {
            productId: 'p1',
            productName: 'Pizza Calabresa',
            isAvailable: false,
            unavailableReason: 'Acabou o insumo',
            unavailableSince: '2026-08-02T20:00:00.000Z',
          },
        ],
      })),
      markAvailable,
    } as unknown as AvailabilityApi;

    const { container } = render(<UnavailableListPage api={api} subscribeFn={noopSubscribe} />);
    const page = within(container);

    await page.findByText('Pizza Calabresa');
    fireEvent.click(page.getByRole('button', { name: 'Marcar disponível' }));

    await waitFor(() => expect(markAvailable).toHaveBeenCalledWith('p1'));
    await waitFor(() => expect(page.queryByText('Pizza Calabresa')).not.toBeInTheDocument());
    expect(await page.findByText('Nenhum item indisponível')).toBeInTheDocument();
  });

  it('reflete em tempo real uma marcação feita a partir do KDS', async () => {
    let capturedOnChange: ((event: ProductAvailabilityChangedEvent) => void) | undefined;
    const subscribeFn = vi.fn((onChange: (event: ProductAvailabilityChangedEvent) => void) => {
      capturedOnChange = onChange;
      return { close: () => undefined };
    }) as unknown as typeof noopSubscribe;
    const api = {
      listUnavailable: vi.fn(async () => ({ items: [] })),
    } as unknown as AvailabilityApi;

    const { container } = render(<UnavailableListPage api={api} subscribeFn={subscribeFn} />);
    const page = within(container);

    await page.findAllByText('Nenhum item indisponível');
    expect(capturedOnChange).toBeDefined();

    act(() => {
      capturedOnChange!({
        type: 'product.unavailable',
        data: { productId: 'p2', reason: 'Marcado pela cozinha' },
      });
    });

    expect(await page.findByText('Marcado pela cozinha')).toBeInTheDocument();
  });

  it('exibe erro quando o carregamento falha', async () => {
    const api = {
      listUnavailable: vi.fn(async () => {
        throw new Error('Não foi possível carregar os itens indisponíveis.');
      }),
    } as unknown as AvailabilityApi;

    const { container } = render(<UnavailableListPage api={api} subscribeFn={noopSubscribe} />);
    const page = within(container);

    expect(
      await page.findByText('Não foi possível carregar os itens indisponíveis.'),
    ).toBeInTheDocument();
  });
});
