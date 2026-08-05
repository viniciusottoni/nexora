// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { BillResponse } from '@nexora/contracts';
import { BillingApi } from './billing-api.js';
import { CheckoutPanel } from './checkout-panel.js';

const identity = {
  accessToken: 'access-local',
  deviceId: '0198aabb-1111-7000-8000-000000000001',
  deviceSecret: 'segredo-local',
};

const bill = {
  items: [],
  subtotal: '100.00',
  serviceFee: '10.00',
  total: '110.00',
  splitMode: 'SINGLE',
  split: [],
  pendingItems: [],
  hasPendingItems: false,
  amountPaid: null,
  remainingAmount: null,
  unassignedItemIds: [],
  serviceFeeOptional: true,
  serviceFeeWaived: false,
} as unknown as BillResponse;

afterEach(() => cleanup());

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

describe('CheckoutPanel', () => {
  it('registra o pagamento único que bate com o total e mostra a confirmação (US-052)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        session: { status: 'CLOSED' },
        payments: [
          {
            id: '0198aabb-4444-7000-8000-000000000001',
            method: 'CASH',
            amount: '110.00',
            netAmount: '110.00',
            feeAmount: '0.00',
            changeAmount: '0.00',
            provider: null,
            providerRef: null,
            reconciliationStatus: 'NOTAPPLICABLE',
          },
        ],
        change: '0.00',
        receipt: { url: '/v1/sessions/session-1/receipt' },
      }),
    );
    const api = new BillingApi('', fetcher);

    render(<CheckoutPanel identity={identity} sessionId="session-1" bill={bill} api={api} />);

    const amountInput = screen.getByLabelText('Valor');
    fireEvent.change(amountInput, { target: { value: '110' } });

    const submitButton = screen.getByRole('button', { name: 'Registrar pagamentos e fechar conta' });
    expect(submitButton).not.toBeDisabled();
    fireEvent.click(submitButton);

    expect(await screen.findByText('Conta fechada')).toBeInTheDocument();
    expect(screen.getByText(/CLOSED/)).toBeInTheDocument();
  });

  it('bloqueia o envio quando a soma não bate com o total', () => {
    const fetcher = vi.fn();
    const api = new BillingApi('', fetcher);

    render(<CheckoutPanel identity={identity} sessionId="session-1" bill={bill} api={api} />);

    fireEvent.change(screen.getByLabelText('Valor'), { target: { value: '50' } });

    expect(screen.getByRole('button', { name: 'Registrar pagamentos e fechar conta' })).toBeDisabled();
    expect(screen.getByText(/Faltam/)).toBeInTheDocument();
  });

  it('registra pagamento de maquininha com provedor, referência, bandeira, parcelas e mostra valor líquido (US-058)', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const payload = JSON.parse(init?.body as string) as {
        payments: Array<{ method: string; amount: number; provider: string; providerRef: string; brand: string; installments: number }>;
      };
      expect(requestUrl(input)).toContain('/v1/sessions/session-1/payments');
      expect(payload.payments[0]).toMatchObject({
        method: 'CREDIT',
        amount: 110,
        provider: 'CIELO',
        providerRef: '123456',
        brand: 'VISA',
        installments: 2,
      });
      return jsonResponse({
        session: { status: 'CLOSED' },
        payments: [
          {
            id: '0198aabb-4444-7000-8000-000000000002',
            method: 'CREDIT',
            amount: '110.00',
            netAmount: '106.92',
            feeAmount: '3.08',
            changeAmount: '0.00',
            provider: 'CIELO',
            providerRef: '123456',
            reconciliationStatus: 'PENDING',
          },
        ],
        change: '0.00',
        receipt: { url: '/v1/sessions/session-1/receipt' },
      });
    });
    const api = new BillingApi('', fetcher);

    render(<CheckoutPanel identity={identity} sessionId="session-1" bill={bill} api={api} />);

    fireEvent.change(screen.getByLabelText('Forma de pagamento'), { target: { value: 'CREDIT' } });
    fireEvent.change(screen.getByLabelText('Valor'), { target: { value: '110' } });
    fireEvent.change(screen.getByLabelText('Maquininha'), { target: { value: 'CIELO' } });
    fireEvent.change(screen.getByLabelText('NSU / referência da transação'), { target: { value: '123456' } });
    fireEvent.change(screen.getByLabelText('Bandeira'), { target: { value: 'VISA' } });
    fireEvent.change(screen.getByLabelText('Parcelas'), { target: { value: '2' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar pagamentos e fechar conta' }));

    expect(await screen.findByText(/líquido R\$ 106,92/)).toBeInTheDocument();
    expect(screen.getByText(/conciliação pendente/)).toBeInTheDocument();
    expect(screen.getByText(/não substitui NFC-e\/SAT/)).toBeInTheDocument();
  });

  it('exige confirmação explícita quando a referência da maquininha parece duplicada (US-058)', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse({ detail: 'Referência duplicada.', code: 'PAYMENT_DUPLICATE_REFERENCE' }, 422))
      .mockImplementationOnce(async (_input: RequestInfo | URL, init?: RequestInit) => {
        const payload = JSON.parse(init?.body as string) as { payments: Array<{ confirmDuplicate?: boolean }> };
        expect(payload.payments[0]?.confirmDuplicate).toBe(true);
        return jsonResponse({
          session: { status: 'CLOSED' },
          payments: [
            {
              id: '0198aabb-4444-7000-8000-000000000003',
              method: 'DEBIT',
              amount: '110.00',
              netAmount: '108.35',
              feeAmount: '1.65',
              changeAmount: '0.00',
              provider: 'CIELO',
              providerRef: '999999',
              reconciliationStatus: 'PENDING',
            },
          ],
          change: '0.00',
          receipt: { url: '/v1/sessions/session-1/receipt' },
        });
      });
    const api = new BillingApi('', fetcher);

    render(<CheckoutPanel identity={identity} sessionId="session-1" bill={bill} api={api} />);

    fireEvent.change(screen.getByLabelText('Forma de pagamento'), { target: { value: 'DEBIT' } });
    fireEvent.change(screen.getByLabelText('Valor'), { target: { value: '110' } });
    fireEvent.change(screen.getByLabelText('Maquininha'), { target: { value: 'CIELO' } });
    fireEvent.change(screen.getByLabelText('NSU / referência da transação'), { target: { value: '999999' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar pagamentos e fechar conta' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Referência já usada');

    fireEvent.click(screen.getByRole('button', { name: 'Confirmar referência duplicada e fechar' }));

    expect(await screen.findByText(/Conta fechada/)).toBeInTheDocument();
  });

  it('desconto acima do limite pede PIN de autorização (US-054)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ detail: 'Autorização necessária.', code: 'AUTHORIZATION_REQUIRED' }, 403),
    );
    const api = new BillingApi('', fetcher);

    render(<CheckoutPanel identity={identity} sessionId="session-1" bill={bill} api={api} />);

    fireEvent.change(screen.getByLabelText('Percentual'), { target: { value: '15' } });
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'cortesia' } });
    fireEvent.click(screen.getByRole('button', { name: 'Aplicar desconto' }));

    await waitFor(() => expect(screen.getByLabelText('PIN do gerente')).toBeInTheDocument());
  });

  it('retirada da taxa (FULL) chama o endpoint autoritativo de US-053 e atualiza o total local', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ session: { serviceFee: '0.00', total: '100.00' } }));
    const api = new BillingApi('', fetcher);
    const onBillChanged = vi.fn();

    render(<CheckoutPanel identity={identity} sessionId="session-1" bill={bill} api={api} onBillChanged={onBillChanged} />);

    fireEvent.click(screen.getByRole('button', { name: 'Retirar taxa de serviço (conta toda)' }));

    await waitFor(() => {
      const [url] = fetcher.mock.calls[0] as unknown as [string];
      expect(url).toContain('/service-fee/waive');
    });
    expect(onBillChanged).toHaveBeenCalledWith({ serviceFee: '0.00', serviceFeeWaived: true, total: '100.00' });
  });
});
