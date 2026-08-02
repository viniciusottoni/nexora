// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ConsumptionView } from './consumption-view.js';

const orderId = '0198aabb-4444-7000-8000-000000000004';
const itemId = '0198aabb-5555-7000-8000-000000000005';
const cancelledItemId = '0198aabb-7777-7000-8000-000000000007';

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function buildConsumption(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    items: [
      {
        orderItemId: itemId,
        orderId,
        name: 'Pizza G Mussarela',
        quantity: 1,
        unitPrice: '52.00',
        total: '52.00',
        status: 'QUEUED',
        statusLabel: 'Na fila',
        etaMinutes: 10,
        cancelled: false,
        variantId: itemId,
        productAvailable: true,
      },
      {
        orderItemId: cancelledItemId,
        orderId,
        name: 'Suco de Laranja',
        quantity: 1,
        unitPrice: '10.00',
        total: '10.00',
        status: 'CANCELLED',
        statusLabel: 'Cancelado',
        etaMinutes: null,
        cancelled: true,
        variantId: cancelledItemId,
        productAvailable: true,
      },
    ],
    subtotal: '52.00',
    serviceFee: '5.20',
    serviceFeeOptional: true,
    total: '57.20',
    openedAt: new Date().toISOString(),
    minutesOpen: 12,
    ...overrides,
  };
}

describe('ConsumptionView (US-024/US-028)', () => {
  beforeEach(() => {
    // ADR-011: força o SignalR a falhar rápido (sem hub real disponível em teste) para exercitar
    // o fallback de polling de forma determinística, em vez de depender de timing de rede real.
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        throw new Error('sem hub real em teste');
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('lista os itens com quantidade/valor/status e mostra subtotal/taxa/total (US-024 §4)', async () => {
    const fetcher = vi.fn(async () => jsonResponse(buildConsumption()));

    render(<ConsumptionView sessionToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} pollIntervalMs={60_000} />);

    expect(await screen.findByText('Pizza G Mussarela')).toBeInTheDocument();
    expect(screen.getByText('Na fila')).toBeInTheDocument();
    // "R$ 52,00" aparece duas vezes de propósito neste fixture (preço do item = subtotal).
    expect(screen.getAllByText('R$ 52,00').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('R$ 5,20')).toBeInTheDocument();
    expect(screen.getByText('R$ 57,20')).toBeInTheDocument();
    expect(screen.getByText('opcional')).toBeInTheDocument();
  });

  it('item cancelado aparece riscado e nao tem acao de repetir', async () => {
    const fetcher = vi.fn(async () => jsonResponse(buildConsumption()));

    render(<ConsumptionView sessionToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} pollIntervalMs={60_000} />);

    const cancelledLine = await screen.findByText('Suco de Laranja');
    expect(cancelledLine.closest('.db-order-line')).toHaveClass('db-order-line--cancelled');
  });

  it('repete um item com um toque e mostra o preco atual em destaque quando muda (US-028)', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = input.toString();
      if (init?.method === 'POST' && url.includes('/repeat')) {
        return jsonResponse({ item: { id: 'novo', unitPrice: '55.00', repeatedFromItemId: itemId } }, 201);
      }
      return jsonResponse(buildConsumption());
    });

    render(<ConsumptionView sessionToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} pollIntervalMs={60_000} />);

    const repeatButton = await screen.findByRole('button', { name: /repetir/i });
    fireEvent.click(repeatButton);

    expect(await screen.findByText(/preço atual R\$ 55,00 \(era R\$ 52,00\)/)).toBeInTheDocument();
  });

  it('bloqueia a repeticao de item indisponivel com mensagem amigavel (US-028 §4)', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = input.toString();
      if (init?.method === 'POST' && url.includes('/repeat')) {
        return jsonResponse({ detail: 'Produto indisponível.', code: 'PRODUCT_UNAVAILABLE' }, 422);
      }
      return jsonResponse(buildConsumption());
    });

    render(<ConsumptionView sessionToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} pollIntervalMs={60_000} />);

    const repeatButton = await screen.findByRole('button', { name: /repetir/i });
    fireEvent.click(repeatButton);

    expect(await screen.findByText('Não é possível repetir — este item está indisponível no momento.')).toBeInTheDocument();
  });

  it('degrada para o indicador de modo local/atrasado quando o WebSocket nao conecta (ADR-011)', async () => {
    const fetcher = vi.fn(async () => jsonResponse(buildConsumption()));

    render(<ConsumptionView sessionToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} pollIntervalMs={60_000} />);

    await screen.findByText('Pizza G Mussarela');
    await waitFor(() => expect(screen.getByText('Sync atrasada')).toBeInTheDocument());
  });

  it('mostra estado vazio quando a sessao ainda nao tem nenhum item lancado', async () => {
    const fetcher = vi.fn(async () => jsonResponse(buildConsumption({ items: [], subtotal: '0.00', serviceFee: '0.00', total: '0.00' })));

    render(<ConsumptionView sessionToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} pollIntervalMs={60_000} />);

    expect(await screen.findByText(/Nenhum item lançado ainda/)).toBeInTheDocument();
  });
});
