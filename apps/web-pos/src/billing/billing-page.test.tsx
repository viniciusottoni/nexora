// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { BillingPage } from './billing-page.js';

const identity = {
  accessToken: 'access-local',
  deviceId: '0198aabb-1111-7000-8000-000000000001',
  deviceSecret: 'segredo-local',
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

const byPersonBill = {
  items: [],
  subtotal: '100.00',
  serviceFee: '0.00',
  total: '100.00',
  splitMode: 'BY_PERSON',
  split: [
    { person: 1, amount: '33.34', serviceFeeAmount: '0.00', serviceFeeWaived: false },
    { person: 2, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
    { person: 3, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
  ],
  pendingItems: [],
  hasPendingItems: false,
  amountPaid: null,
  remainingAmount: null,
  unassignedItemIds: [],
};

afterEach(() => cleanup());

describe('BillingPage', () => {
  it('carrega a divisão por pessoa (padrão) com resíduo de arredondamento (US-027 §4)', async () => {
    const fetcher = vi.fn(async () => jsonResponse(byPersonBill));

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    expect(await screen.findByText('R$ 33,34')).toBeInTheDocument();
    expect(screen.getAllByText(/R\$ 33,33/)).toHaveLength(2);
  });

  it('avisa quando há item ainda em produção (RN-017)', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ ...byPersonBill, hasPendingItems: true }));

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    expect(await screen.findByRole('alert')).toHaveTextContent('ainda em produção');
  });

  it('troca para o modo por valor e registra um pagamento parcial (US-027 §4, cenário "Divisão por valor")', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (init?.method === 'POST' && url.includes('/partial-payment')) {
        return jsonResponse({
          paymentId: '0198aabb-4444-7000-8000-000000000001',
          amountPaid: '50.00',
          remainingAmount: '130.00',
          total: '180.00',
          sessionStatus: 'BILLREQUESTED',
        });
      }
      if (url.includes('split=BY_AMOUNT')) {
        return jsonResponse({
          ...byPersonBill,
          splitMode: 'BY_AMOUNT',
          subtotal: '180.00',
          total: '180.00',
          split: [],
          amountPaid: null,
          remainingAmount: '180.00',
        });
      }
      return jsonResponse(byPersonBill);
    });

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    await screen.findByText('R$ 33,34');
    fireEvent.click(screen.getByRole('button', { name: 'Por valor' }));

    const amountInput = await screen.findByLabelText('Valor pago agora');
    fireEvent.change(amountInput, { target: { value: '50' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar pagamento' }));

    await waitFor(() =>
      expect(screen.getByText(/Restam R\$ 130,00 em aberto/)).toBeInTheDocument(),
    );
  });

  // US-035 (Bloquear fechamento com item pendente) — os três modos configuráveis (RN-017).
  const pendingItem = { id: '0198aabb-6666-7000-8000-000000000001', name: 'Petit Gateau', status: 'READY' };

  it('modo BLOCK desabilita o botão de pagamento e lista os itens pendentes (cenário "Fechamento bloqueado")', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes('split=BY_AMOUNT')) {
        return jsonResponse({
          ...byPersonBill,
          splitMode: 'BY_AMOUNT',
          pendingItems: [pendingItem],
          hasPendingItems: true,
          pendingItemsMode: 'BLOCK',
          remainingAmount: '100.00',
        });
      }
      return jsonResponse({ ...byPersonBill, pendingItems: [pendingItem], hasPendingItems: true, pendingItemsMode: 'BLOCK' });
    });

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Por valor' }));

    expect(await screen.findByText(/Fechamento bloqueado/)).toBeInTheDocument();
    expect(screen.getByText(/Petit Gateau/)).toBeInTheDocument();
    expect(screen.getByText('READY')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Registrar pagamento' })).toBeDisabled();
  });

  it('modo BLOCK reabilita o pagamento depois de autorizar com PIN e motivo (cenário "Fechamento autorizado mesmo com pendência")', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url.includes('/v1/auth/authorize')) {
        return jsonResponse({
          authorizationToken: 'token-autorizacao',
          expiresIn: 120,
          authorizedBy: { id: '0198aabb-7777-7000-8000-000000000001', name: 'Gerente Ana' },
        });
      }
      if (init?.method === 'POST' && url.includes('/partial-payment')) {
        expect(new Headers(init.headers).get('X-Authorization-Token')).toBe('token-autorizacao');
        return jsonResponse({
          paymentId: '0198aabb-4444-7000-8000-000000000002',
          amountPaid: '50.00',
          remainingAmount: '50.00',
          total: '100.00',
          sessionStatus: 'BILLREQUESTED',
        });
      }
      if (url.includes('split=BY_AMOUNT')) {
        return jsonResponse({
          ...byPersonBill,
          splitMode: 'BY_AMOUNT',
          pendingItems: [pendingItem],
          hasPendingItems: true,
          pendingItemsMode: 'BLOCK',
          remainingAmount: '100.00',
        });
      }
      return jsonResponse({ ...byPersonBill, pendingItems: [pendingItem], hasPendingItems: true, pendingItemsMode: 'BLOCK' });
    });

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Por valor' }));
    await screen.findByText(/Fechamento bloqueado/);
    expect(screen.getByRole('button', { name: 'Registrar pagamento' })).toBeDisabled();

    fireEvent.change(screen.getByLabelText('PIN do gerente'), { target: { value: '1234' } });
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Cliente desistiu do item' } });
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar fechamento' }));

    await waitFor(() => expect(screen.getByRole('button', { name: 'Registrar pagamento' })).not.toBeDisabled());

    fireEvent.change(screen.getByLabelText('Valor pago agora'), { target: { value: '50' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar pagamento' }));

    await waitFor(() => expect(screen.getByText(/Restam R\$ 50,00 em aberto/)).toBeInTheDocument());
  });

  it('modo IGNORE não exibe nenhum aviso, mesmo com item pendente', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ ...byPersonBill, pendingItems: [pendingItem], hasPendingItems: true, pendingItemsMode: 'IGNORE' }),
    );

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    await screen.findByText('R$ 33,34');
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.queryByText(/Fechamento bloqueado/)).not.toBeInTheDocument();
  });

  it('modo por item recusa calcular com item órfão e mostra a mensagem estável', async () => {
    const itemId = '0198aabb-5555-7000-8000-000000000001';
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (init?.method === 'POST' && url.includes('/assign-items')) {
        return jsonResponse({ detail: 'Item não atribuído.', code: 'BILL_ITEM_NOT_ASSIGNED' }, 422);
      }
      if (url.includes('split=BY_ITEM')) {
        return jsonResponse({
          ...byPersonBill,
          splitMode: 'BY_ITEM',
          split: [],
          items: [{ id: itemId, name: 'Pizza Marguerita', total: '40.00', pending: false, assignedPerson: null }],
        });
      }
      return jsonResponse(byPersonBill);
    });

    render(<BillingPage identity={identity} sessionId="session-1" fetcher={fetcher} />);

    await screen.findByText('R$ 33,34');
    fireEvent.click(screen.getByRole('button', { name: 'Por item' }));

    await screen.findByText('Pizza Marguerita');
    // Não atribui nenhuma pessoa ao item — clica direto em calcular.
    fireEvent.click(screen.getByRole('button', { name: 'Calcular divisão' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('atribua todos antes de calcular');
  });
});
