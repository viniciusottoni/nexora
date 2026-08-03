// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { OpenTablePage } from './open-table-page.js';

const identity = {
  accessToken: 'access-local',
  deviceId: '0198aabb-1111-7000-8000-000000000001',
  deviceSecret: 'segredo-local',
};

const freeTable = {
  id: '0198aabb-2222-7000-8000-000000000002',
  area: 'Salão',
  label: '12',
  seats: 4,
  status: 'FREE',
  session: null,
  flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false },
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

describe('OpenTablePage', () => {
  it('lista as mesas livres e permite escolher uma', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ tables: [freeTable] }));

    render(<OpenTablePage identity={identity} fetcher={fetcher as unknown as typeof fetch} />);

    expect(await screen.findByText('Mesa 12')).toBeInTheDocument();
  });

  it('mostra estado vazio quando nao ha mesa livre', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ tables: [] }));

    render(<OpenTablePage identity={identity} fetcher={fetcher as unknown as typeof fetch} />);

    expect(await screen.findByText('Nenhuma mesa livre agora')).toBeInTheDocument();
  });

  it('abrir mesa em dois toques: escolher a mesa, confirmar quantidade e some da lista de livres (feedback otimista)', async () => {
    let openCalls = 0;
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = input.toString();
      if (init?.method === 'POST' && url.includes('/sessions')) {
        openCalls += 1;
        return jsonResponse(
          {
            id: '0198aabb-4444-7000-8000-000000000001',
            tableId: freeTable.id,
            tableLabel: freeTable.label,
            status: 'OPEN',
            openedAt: new Date().toISOString(),
            guestCount: 4,
            guestCountConfirmed: true,
            waiterId: null,
            source: 'WAITER',
            currentItems: [],
            total: '0.00',
          },
          201,
        );
      }
      return jsonResponse({ tables: [freeTable] });
    });

    render(<OpenTablePage identity={identity} fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(await screen.findByText('Mesa 12'));
    expect(await screen.findByText('Quantas pessoas sentaram?')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Abrir mesa' }));

    await waitFor(() => expect(openCalls).toBe(1));
    await waitFor(() => expect(screen.getByText('Nenhuma mesa livre agora')).toBeInTheDocument());
  });

  it('mesa ja ocupada por outra requisicao (409) mostra mensagem amigavel e volta a lista', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = input.toString();
      if (init?.method === 'POST' && url.includes('/sessions')) {
        return jsonResponse({ detail: 'Esta mesa já tem uma comanda em aberto.', code: 'TABLE_ALREADY_OPEN' }, 409);
      }
      return jsonResponse({ tables: [freeTable] });
    });

    render(<OpenTablePage identity={identity} fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.click(await screen.findByText('Mesa 12'));
    fireEvent.click(screen.getByRole('button', { name: 'Abrir mesa' }));

    expect(await screen.findByText('Esta mesa já tem uma comanda em aberto.')).toBeInTheDocument();
  });
});
