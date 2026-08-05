// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { WaiterCallPanel } from './waiter-call-panel.js';

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

function buildConsumption(subtotal = '80.00') {
  return {
    items: [],
    subtotal,
    serviceFee: '0.00',
    serviceFeeOptional: true,
    total: subtotal,
    openedAt: new Date().toISOString(),
    minutesOpen: 5,
  };
}

function buildBillPreview(people: number, subtotal = '80.00') {
  const per = (Number.parseFloat(subtotal) / people).toFixed(2);
  return {
    items: [],
    subtotal,
    serviceFee: '0.00',
    total: subtotal,
    splitMode: 'BY_PERSON',
    split: Array.from({ length: people }, (_, index) => ({
      person: index + 1,
      amount: per,
      serviceFeeAmount: '0.00',
      serviceFeeWaived: false,
    })),
    pendingItems: [],
    hasPendingItems: false,
    amountPaid: null,
    remainingAmount: null,
    unassignedItemIds: [],
  };
}

describe('WaiterCallPanel (US-025/US-026)', () => {
  it('chama o garcom e mostra confirmacao visual imediata', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      expect(requestUrl(input)).toContain('/call-waiter');
      return jsonResponse({ acknowledged: true, alreadyPending: false });
    });

    render(<WaiterCallPanel qrToken="token-mesa-12" sessionToken="token-de-sessao" fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(screen.getByRole('button', { name: /chamar garçom/i }));

    expect(await screen.findByText('Garçom chamado! Ele já está a caminho.')).toBeInTheDocument();
  });

  it('cenario "chamada repetida": mostra que o garcom ja foi avisado', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ acknowledged: true, alreadyPending: true }));

    render(<WaiterCallPanel qrToken="token-mesa-12" sessionToken="token-de-sessao" fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(screen.getByRole('button', { name: /chamar garçom/i }));

    expect(await screen.findByText('O garçom já foi avisado — só um instante.')).toBeInTheDocument();
  });

  it('abre a tela de pedir a conta e mostra o valor por pessoa calculado pelo backend (US-027 §10)', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes('/sessions/current/bill')) {
        expect(url).toContain('split=BY_PERSON');
        expect(url).toContain('people=4');
        return jsonResponse(buildBillPreview(4, '80.00'));
      }
      if (url.includes('/sessions/current')) {
        return jsonResponse(buildConsumption('80.00'));
      }
      return jsonResponse({ session: {}, alreadyRequested: false });
    });

    render(<WaiterCallPanel qrToken="token-mesa-12" sessionToken="token-de-sessao" fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(screen.getByRole('button', { name: /pedir a conta/i }));
    fireEvent.click(await screen.findByRole('radio', { name: /dividir por pessoa/i }));

    const peopleInput = await screen.findByLabelText(/quantas pessoas/i);
    fireEvent.change(peopleInput, { target: { value: '4' } });

    expect(await screen.findByText(/R\$ 20,00 por pessoa/)).toBeInTheDocument();
  });

  it('mostra o valor da 1ª pessoa separado quando o resíduo de arredondamento (ADR-017) gera um valor diferente', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes('/sessions/current/bill')) {
        return jsonResponse({
          ...buildBillPreview(3, '100.00'),
          split: [
            { person: 1, amount: '33.34', serviceFeeAmount: '0.00', serviceFeeWaived: false },
            { person: 2, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
            { person: 3, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
          ],
        });
      }
      if (url.includes('/sessions/current')) {
        return jsonResponse(buildConsumption('100.00'));
      }
      return jsonResponse({ session: {}, alreadyRequested: false });
    });

    render(<WaiterCallPanel qrToken="token-mesa-12" sessionToken="token-de-sessao" fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(screen.getByRole('button', { name: /pedir a conta/i }));
    fireEvent.click(await screen.findByRole('radio', { name: /dividir por pessoa/i }));

    const peopleInput = await screen.findByLabelText(/quantas pessoas/i);
    fireEvent.change(peopleInput, { target: { value: '3' } });

    expect(await screen.findByText(/R\$ 33,34 para a 1ª pessoa e R\$ 33,33 para as demais/)).toBeInTheDocument();
  });

  it('confirma a solicitacao de conta enviando o modo escolhido', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url.includes('/sessions/current')) {
        return jsonResponse(buildConsumption());
      }
      expect(url).toContain('/request-bill');
      const body = JSON.parse(init?.body as string) as Record<string, unknown>;
      expect(body.splitMode).toBe('SINGLE');
      return jsonResponse({
        session: {
          id: '0198aabb-3333-7000-8000-000000000003',
          tableId: '0198aabb-2222-7000-8000-000000000002',
          tableLabel: '12',
          status: 'BILLREQUESTED',
          openedAt: new Date().toISOString(),
          guestCount: 1,
          guestCountConfirmed: true,
          waiterId: null,
          source: 'QR',
          currentItems: [],
          total: '80.00',
        },
        alreadyRequested: false,
      });
    });

    render(<WaiterCallPanel qrToken="token-mesa-12" sessionToken="token-de-sessao" fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(screen.getByRole('button', { name: /pedir a conta/i }));
    fireEvent.click(await screen.findByRole('button', { name: /confirmar/i }));

    expect(await screen.findByText('Conta solicitada! O caixa já foi avisado.')).toBeInTheDocument();
  });
});
