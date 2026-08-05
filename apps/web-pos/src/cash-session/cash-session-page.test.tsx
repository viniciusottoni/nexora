// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CashSessionPage } from './cash-session-page.js';

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

const baseSession = {
  id: '0198aabb-2222-7000-8000-000000000001',
  operatorId: '0198aabb-2222-7000-8000-000000000002',
  status: 'OPEN',
  openingAmount: '200.00',
  openedAt: '2026-08-05T10:00:00Z',
  closedAt: null,
  expectedAmount: null,
  countedAmount: null,
  divergence: null,
  justification: null,
};

const compositionExpected = {
  opening: '200.00',
  cashPayments: '1500.00',
  supplies: '300.00',
  withdrawals: '-150.00',
  total: '1850.00',
};

afterEach(() => cleanup());

describe('CashSessionPage', () => {
  it('mostra o formulário de abertura quando não há caixa aberto e abre com o fundo informado (US-055 §4, "Abertura com fundo")', async () => {
    let opened = false;
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url.includes('/v1/cash-sessions/open')) {
        opened = true;
        return jsonResponse({ session: { ...baseSession } }, 201);
      }
      if (url.includes('/v1/cash-sessions/current/movements')) {
        return jsonResponse({ movements: [] });
      }
      if (url.includes('/v1/cash-sessions/current')) {
        if (!opened) {
          return jsonResponse({ detail: 'Não há caixa aberto.', code: 'NO_OPEN_CASH_SESSION' }, 409);
        }
        return jsonResponse({ session: baseSession, expected: { opening: '200.00', cashPayments: '0.00', supplies: '0.00', withdrawals: '0.00', total: '200.00' } });
      }
      throw new Error(`unexpected request: ${init?.method ?? 'GET'} ${url}`);
    });

    render(<CashSessionPage identity={identity} fetcher={fetcher} />);

    expect(await screen.findByRole('heading', { name: 'Abrir caixa' })).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Fundo de caixa'), { target: { value: '200' } });
    fireEvent.click(screen.getByRole('button', { name: 'Abrir caixa' }));

    expect(await screen.findByText('Valor esperado')).toBeInTheDocument();
    expect(screen.getAllByText('R$ 200,00')).toHaveLength(2); // fundo de abertura + total esperado (composição vazia)
  });

  it('exibe a composição detalhada do valor esperado (US-055 §4, "Composição do valor esperado")', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes('/movements')) return jsonResponse({ movements: [] });
      if (url.includes('/current')) return jsonResponse({ session: baseSession, expected: compositionExpected });
      throw new Error(`unexpected request: ${url}`);
    });

    render(<CashSessionPage identity={identity} fetcher={fetcher} />);

    expect(await screen.findByText('R$ 1.850,00')).toBeInTheDocument();
    expect(screen.getByText('R$ 1.500,00')).toBeInTheDocument();
    expect(screen.getByText('R$ 300,00')).toBeInTheDocument();
    expect(screen.getByText('-R$ 150,00')).toBeInTheDocument();
  });

  it('sangria acima do limite pede autorização por PIN e conclui após autorizar (US-056 §4, "Sangria acima do limite")', async () => {
    let authorized = false;
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url.includes('/v1/auth/authorize')) {
        authorized = true;
        return jsonResponse({
          authorizationToken: 'token-sangria',
          expiresIn: 120,
          authorizedBy: { id: '0198aabb-7777-7000-8000-000000000001', name: 'Gerente Ana' },
        });
      }
      if (init?.method === 'POST' && url.includes('/v1/cash-sessions/movements')) {
        if (!authorized) {
          return jsonResponse({ detail: 'Autorização necessária.', code: 'AUTHORIZATION_REQUIRED' }, 403);
        }
        expect(new Headers(init.headers).get('X-Authorization-Token')).toBe('token-sangria');
        return jsonResponse({
          movement: {
            id: '0198aabb-3333-7000-8000-000000000001',
            type: 'WITHDRAWAL',
            amount: '800.00',
            reason: 'Depósito no banco',
            occurredAt: '2026-08-05T12:00:00Z',
            createdBy: identity.deviceId,
            authorizedBy: '0198aabb-7777-7000-8000-000000000001',
          },
          newExpected: '700.00',
        });
      }
      if (url.includes('/movements')) return jsonResponse({ movements: [] });
      if (url.includes('/current')) return jsonResponse({ session: baseSession, expected: compositionExpected });
      throw new Error(`unexpected request: ${init?.method ?? 'GET'} ${url}`);
    });

    render(<CashSessionPage identity={identity} fetcher={fetcher} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Retirar (sangria)' }));
    fireEvent.change(screen.getByLabelText('Valor'), { target: { value: '800' } });
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Depósito no banco' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar sangria' }));

    expect(await screen.findByLabelText('PIN do gerente')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('PIN do gerente'), { target: { value: '4433' } });
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar sangria' }));

    await waitFor(() => expect(screen.getByText('Autorizado')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Confirmar sangria' }));

    await waitFor(() =>
      expect(screen.getByText(/Sangria de R\$ 800,00 registrada/)).toBeInTheDocument(),
    );
  });

  it('mesa aberta bloqueia o fechamento e a autorização por PIN permite prosseguir (US-055 §4, RN-018)', async () => {
    let authorized = false;
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url.includes('/v1/auth/authorize')) {
        authorized = true;
        return jsonResponse({
          authorizationToken: 'token-mesa',
          expiresIn: 120,
          authorizedBy: { id: '0198aabb-7777-7000-8000-000000000001', name: 'Gerente Ana' },
        });
      }
      if (init?.method === 'POST' && url.includes('/close')) {
        if (!authorized) {
          return jsonResponse(
            { detail: 'Existem mesas ainda abertas.', code: 'OPEN_TABLES', meta: { openSessions: [{ table: '12', total: '87.00' }] } },
            422,
          );
        }
        expect(new Headers(init.headers).get('X-Authorization-Token')).toBe('token-mesa');
        return jsonResponse({
          expected: '200.00',
          counted: '200.00',
          divergence: '0.00',
          requiresJustification: false,
          session: { ...baseSession, status: 'CLOSED', closedAt: '2026-08-05T22:00:00Z' },
        });
      }
      if (url.includes('/movements')) return jsonResponse({ movements: [] });
      if (url.includes('/current')) {
        return jsonResponse({ session: baseSession, expected: { opening: '200.00', cashPayments: '0.00', supplies: '0.00', withdrawals: '0.00', total: '200.00' } });
      }
      throw new Error(`unexpected request: ${init?.method ?? 'GET'} ${url}`);
    });

    render(<CashSessionPage identity={identity} fetcher={fetcher} />);

    fireEvent.change(await screen.findByLabelText('Valor contado'), { target: { value: '200' } });
    fireEvent.click(screen.getByRole('button', { name: 'Fechar caixa' }));

    expect(await screen.findByText('Mesas ainda abertas')).toBeInTheDocument();
    expect(screen.getByText(/Mesa 12/)).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('PIN do gerente'), { target: { value: '9911' } });
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar fechamento' }));

    await waitFor(() => expect(screen.getByText('Autorizado')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Fechar caixa' }));

    expect(await screen.findByRole('heading', { name: 'Relatório de fechamento' })).toBeInTheDocument();
  });

  it('divergência acima do limiar exige justificativa antes de concluir (US-055 §4, "Divergência no fechamento")', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input);
      if (init?.method === 'POST' && url.includes('/close')) {
        const body = JSON.parse(init.body as string) as { justification?: string | null };
        if (!body.justification) {
          return jsonResponse({ detail: 'Justificativa exigida.', code: 'CASH_JUSTIFICATION_REQUIRED' }, 422);
        }
        return jsonResponse({
          expected: '1850.00',
          counted: '1843.50',
          divergence: '-6.50',
          requiresJustification: true,
          session: { ...baseSession, status: 'CLOSED', justification: body.justification },
        });
      }
      if (url.includes('/movements')) return jsonResponse({ movements: [] });
      if (url.includes('/current')) return jsonResponse({ session: baseSession, expected: compositionExpected });
      throw new Error(`unexpected request: ${init?.method ?? 'GET'} ${url}`);
    });

    render(<CashSessionPage identity={identity} fetcher={fetcher} />);

    fireEvent.change(await screen.findByLabelText('Valor contado'), { target: { value: '1843.50' } });
    fireEvent.click(screen.getByRole('button', { name: 'Fechar caixa' }));

    expect(await screen.findByLabelText('Justificativa')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Justificativa'), { target: { value: 'Troco entregue a mais' } });
    fireEvent.click(screen.getByRole('button', { name: 'Fechar caixa' }));

    expect(await screen.findByRole('heading', { name: 'Relatório de fechamento' })).toBeInTheDocument();
    expect(screen.getByText(/Justificativa: Troco entregue a mais/)).toBeInTheDocument();
  });
});
