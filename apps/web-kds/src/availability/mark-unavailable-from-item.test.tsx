// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { AvailabilityApi } from './availability-api.js';
import { MarkUnavailableFromItem } from './mark-unavailable-from-item.js';

describe('MarkUnavailableFromItem (US-044 §3/§6)', () => {
  afterEach(() => {
    cleanup();
  });

  it('abre o diálogo numerado e marca indisponível passando o orderItemId de origem', async () => {
    const markUnavailable = vi.fn(async () => ({
      productId: 'p1',
      productName: 'Pizza Calabresa',
      isAvailable: false,
      unavailableReason: 'OUT_OF_STOCK',
      unavailableSince: '2026-08-02T20:00:00.000Z',
    }));
    const api = { markUnavailable } as unknown as AvailabilityApi;

    render(
      <MarkUnavailableFromItem productId="p1" productName="Pizza Calabresa" orderItemId="item-1" api={api} />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Marcar Pizza Calabresa como indisponível' }));
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: '1 Acabou' }));

    await waitFor(() => expect(markUnavailable).toHaveBeenCalledWith('p1', 'OUT_OF_STOCK', true, 'item-1'));
    expect(await screen.findByText('Sinalizado')).toBeInTheDocument();
  });

  it('cancelar não chama a API', () => {
    const markUnavailable = vi.fn();
    const api = { markUnavailable } as unknown as AvailabilityApi;

    render(
      <MarkUnavailableFromItem productId="p1" productName="Pizza Calabresa" orderItemId="item-1" api={api} />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Marcar Pizza Calabresa como indisponível' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }));

    expect(markUnavailable).not.toHaveBeenCalled();
  });

  it('mostra erro sem travar a tela quando a chamada falha', async () => {
    const markUnavailable = vi.fn(async () => {
      throw new Error('Produto não encontrado.');
    });
    const api = { markUnavailable } as unknown as AvailabilityApi;

    render(
      <MarkUnavailableFromItem productId="p1" productName="Pizza Calabresa" orderItemId="item-1" api={api} />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Marcar Pizza Calabresa como indisponível' }));
    fireEvent.click(screen.getByRole('button', { name: '2 Equipamento' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Produto não encontrado.');
  });
});
