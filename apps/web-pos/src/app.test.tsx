// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PosHome, PosOperationalWorkArea } from './app.js';

vi.mock('./table-map/table-map-page.js', () => ({
  TableMapPage: ({ onOpenBilling }: { onOpenBilling: (sessionId: string) => void }) => (
    <section aria-label="Mapa de mesas">
      <h1>Mapa de mesas</h1>
      <button type="button" onClick={() => onOpenBilling('0198aabb-2222-7000-8000-000000000001')}>
        Dividir a conta
      </button>
    </section>
  ),
}));

vi.mock('./cash-panel/cash-panel-page.js', () => ({
  CashPanelPage: ({ onOpenBilling, mode }: { onOpenBilling: (sessionId: string) => void; mode?: 'overview' | 'receiving' }) => (
    <section aria-label={mode === 'receiving' ? 'Selecao de recebimento' : 'Painel do caixa'}>
      <h1>{mode === 'receiving' ? 'Selecione a conta para receber' : 'Mesas e comandas abertas'}</h1>
      <button type="button" onClick={() => onOpenBilling('0198aabb-3333-7000-8000-000000000001')}>
        {mode === 'receiving' ? 'Receber conta' : 'Dividir a conta'}
      </button>
    </section>
  ),
}));

vi.mock('./cash-session/cash-session-page.js', () => ({
  CashSessionPage: () => (
    <section aria-label="Caixa">
      <h1>Fechamento e movimentos</h1>
    </section>
  ),
}));

vi.mock('./billing/billing-page.js', () => ({
  BillingPage: ({ sessionId }: { sessionId: string }) => (
    <section aria-label="Fechamento da comanda">
      <h1>Dividir a conta</h1>
      <p>{sessionId}</p>
    </section>
  ),
}));

vi.mock('./tables/open-table-page.js', () => ({
  OpenTablePage: () => <section aria-label="Abrir mesa">Abrir mesa</section>,
}));

vi.mock('./order-composition/order-composition-page.js', () => ({
  OrderCompositionPage: () => <section aria-label="Lançar pedido">Lançar pedido</section>,
}));

afterEach(() => cleanup());

describe('PosHome', () => {
  it('identifica operação com marca carregada em runtime', () => {
    render(<PosHome tenantName="Casa do Bairro" />);
    expect(screen.getByRole('heading', { name: 'Casa do Bairro' })).toBeInTheDocument();
    expect(screen.getByText('Caixa pronto')).toBeInTheDocument();
  });
});

describe('PosOperationalWorkArea', () => {
  const identity = {
    accessToken: 'access-local',
    deviceId: '0198aabb-1111-7000-8000-000000000001',
    deviceSecret: 'segredo-local',
  };

  it('expõe o painel do caixa e a tela de sessão de caixa no shell autenticado (E-05)', () => {
    render(<PosOperationalWorkArea identity={identity} />);

    expect(screen.getByRole('heading', { name: 'Mapa de mesas' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Painel do caixa' }));
    expect(screen.getByRole('heading', { name: 'Mesas e comandas abertas' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Caixa' }));
    expect(screen.getByRole('heading', { name: 'Fechamento e movimentos' })).toBeInTheDocument();
  });

  it('abre a conta a partir do painel do caixa sem passar pelo mapa de mesas', () => {
    render(<PosOperationalWorkArea identity={identity} />);

    fireEvent.click(screen.getByRole('button', { name: 'Painel do caixa' }));
    fireEvent.click(screen.getByRole('button', { name: 'Dividir a conta' }));

    expect(screen.getByRole('heading', { name: 'Dividir a conta' })).toBeInTheDocument();
    expect(screen.getByText('0198aabb-3333-7000-8000-000000000001')).toBeInTheDocument();
  });

  it('abre Recebimento como selecao de conta mesmo sem uma sessao previamente escolhida', () => {
    render(<PosOperationalWorkArea identity={identity} />);

    fireEvent.click(screen.getByRole('button', { name: 'Recebimento' }));

    expect(screen.getByRole('heading', { name: 'Selecione a conta para receber' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Receber conta' })).toBeInTheDocument();
  });
});
