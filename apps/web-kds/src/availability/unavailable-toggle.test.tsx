// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { AvailabilityApi, type AvailabilitySubscription } from './availability-api.js';
import { UnavailableToggle } from './unavailable-toggle.js';

/** Duplo de assinatura em tempo real que nunca abre WebSocket de verdade — evita ruído em jsdom. */
function noopSubscribe(): AvailabilitySubscription {
  return { close: () => undefined };
}

describe('UnavailableToggle', () => {
  it('produto disponível abre dialog do design system e marca indisponível com o motivo', async () => {
    const markUnavailable = vi.fn(async () => ({
      productId: 'p1',
      productName: 'Pizza Calabresa',
      isAvailable: false,
      unavailableReason: 'Acabou a calabresa',
      unavailableSince: '2026-08-02T20:00:00.000Z',
    }));
    const api = { markUnavailable } as unknown as AvailabilityApi;

    render(
      <UnavailableToggle
        productId="p1"
        productName="Pizza Calabresa"
        isAvailable
        api={api}
        subscribeFn={noopSubscribe}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Marcar indisponível' }));
    expect(screen.getByRole('dialog', { name: 'Marcar produto indisponível' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Acabou a calabresa' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar indisponibilidade' }));

    await waitFor(() => expect(markUnavailable).toHaveBeenCalledWith('p1', 'Acabou a calabresa'));
    expect(await screen.findByText('Em falta — Acabou a calabresa')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Marcar disponível' })).toBeInTheDocument();
  });

  it('cancelar o dialog nao marca indisponivel', async () => {
    const markUnavailable = vi.fn();
    const api = { markUnavailable } as unknown as AvailabilityApi;

    render(
      <UnavailableToggle
        productId="p1"
        productName="Pizza Calabresa"
        isAvailable
        api={api}
        subscribeFn={noopSubscribe}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Marcar indisponível' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }));

    expect(markUnavailable).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Marcar indisponível' })).toBeInTheDocument();
  });

  it('produto indisponível volta a disponível em um toque, sem pedir motivo', async () => {
    const markAvailable = vi.fn(async () => ({
      productId: 'p1',
      productName: 'Pizza Calabresa',
      isAvailable: true,
      unavailableReason: null,
      unavailableSince: null,
    }));
    const api = { markAvailable } as unknown as AvailabilityApi;

    render(
      <UnavailableToggle
        productId="p1"
        productName="Pizza Calabresa"
        isAvailable={false}
        unavailableReason="Acabou o insumo"
        api={api}
        subscribeFn={noopSubscribe}
      />,
    );

    expect(screen.getByText('Em falta — Acabou o insumo')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Marcar disponível' }));

    await waitFor(() => expect(markAvailable).toHaveBeenCalledWith('p1'));
    await waitFor(() => expect(screen.queryByText(/Em falta/)).not.toBeInTheDocument());
  });

  it('reflete em tempo real uma mudanca vinda de outro dispositivo para o MESMO produto', () => {
    let capturedOnChange: ((event: unknown) => void) | undefined;
    const subscribeFn = vi.fn((onChange: (event: unknown) => void) => {
      capturedOnChange = onChange;
      return { close: () => undefined };
    }) as unknown as typeof noopSubscribe;

    render(
      <UnavailableToggle
        productId="p1"
        productName="Pizza Calabresa"
        isAvailable
        subscribeFn={subscribeFn}
      />,
    );

    expect(capturedOnChange).toBeDefined();
    act(() => {
      capturedOnChange!({
        type: 'product.unavailable',
        data: { productId: 'p1', reason: 'Marcado pelo gestor' },
      });
    });

    expect(screen.getByText('Em falta — Marcado pelo gestor')).toBeInTheDocument();
  });

  it('ignora evento em tempo real de outro produto', () => {
    let capturedOnChange: ((event: unknown) => void) | undefined;
    const subscribeFn = vi.fn((onChange: (event: unknown) => void) => {
      capturedOnChange = onChange;
      return { close: () => undefined };
    }) as unknown as typeof noopSubscribe;

    render(
      <UnavailableToggle
        productId="p1"
        productName="Pizza Calabresa"
        isAvailable
        subscribeFn={subscribeFn}
      />,
    );

    act(() => {
      capturedOnChange!({
        type: 'product.unavailable',
        data: { productId: 'outro-produto', reason: 'Nao deveria aparecer' },
      });
    });

    expect(screen.queryByText(/Em falta/)).not.toBeInTheDocument();
  });

  it('exibe erro quando a chamada falha, sem quebrar a tela', async () => {
    const markAvailable = vi.fn(async () => {
      throw new Error('Produto não encontrado.');
    });
    const api = { markAvailable } as unknown as AvailabilityApi;

    render(
      <UnavailableToggle
        productId="p1"
        productName="Pizza Calabresa"
        isAvailable={false}
        api={api}
        subscribeFn={noopSubscribe}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Marcar disponível' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Produto não encontrado.');
  });
});
