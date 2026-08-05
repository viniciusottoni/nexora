// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CashPanelPage } from './cash-panel-page.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

const startMock = vi.fn().mockResolvedValue(undefined);
const stopMock = vi.fn().mockResolvedValue(undefined);
let capturedOptions: {
  onTableChanged: (table: unknown) => void;
  onModeChange?: (mode: 'ws' | 'polling') => void;
  poll: () => void;
} | null = null;

// A conexão SignalR de verdade abriria um socket de rede — fora do escopo de um teste de
// componente (já coberto, sem rede, por table-map-realtime.test.ts). O painel do caixa reaproveita
// o MESMO módulo de conexão do mapa de mesas (mesma fonte de dados, table_session).
vi.mock('../table-map/table-map-realtime.js', () => ({
  createTableMapHubConnection: vi.fn(() => ({})),
  TableMapRealtimeClient: vi.fn().mockImplementation((_connection: unknown, options: typeof capturedOptions) => {
    capturedOptions = options;
    return { start: startMock, stop: stopMock, currentMode: 'ws' as const };
  }),
}));

function sessionFixture(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    sessionId: '0198aabb-1111-7000-8000-000000000001',
    table: '12',
    area: 'Salão',
    openedAt: '2026-08-02T18:00:00.000Z',
    minutesOpen: 47,
    guestCount: 4,
    waiter: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Ana' },
    total: '186.40',
    status: 'OPEN',
    billRequestedAt: null,
    waitingSeconds: null,
    pendingItems: 0,
    orderCode: 'A47',
    ...overrides,
  };
}

function fetchOk(body: unknown) {
  return { ok: true, json: () => Promise.resolve(body) };
}

describe('CashPanelPage', () => {
  beforeEach(() => {
    capturedOptions = null;
    startMock.mockClear();
    stopMock.mockClear();
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it('carrega e mostra as sessões abertas com valor, tempo e garçom', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        fetchOk({
          sessions: [sessionFixture()],
          summary: { openSessions: 1, totalOpen: '186.40' },
        }),
      ),
    );

    render(<CashPanelPage identity={identity} />);

    expect(await screen.findByText('12')).toBeInTheDocument();
    expect(screen.getByText('Ana')).toBeInTheDocument();
    // Aparece duas vezes: no totalizador do salão (StatTile) e na coluna Total da linha — os dois
    // batem porque só há uma sessão aberta neste fixture (mesma asserção do Gherkin "Totalizador
    // bate com a soma das sessões").
    expect(screen.getAllByText('R$ 186,40')).toHaveLength(2);
    expect(screen.getByText('47 min')).toBeInTheDocument();
  });

  it('mostra o totalizador do salão (US-050 §4)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        fetchOk({
          sessions: [sessionFixture(), sessionFixture({ sessionId: '0198aabb-1111-7000-8000-000000000010', table: '5' })],
          summary: { openSessions: 2, totalOpen: '372.80' },
        }),
      ),
    );

    render(<CashPanelPage identity={identity} />);

    await waitFor(() => expect(screen.getByText('Sessões abertas')).toBeInTheDocument());
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('R$ 372,80')).toBeInTheDocument();
  });

  it('US-050 §4 "Prioridade de conta solicitada": destaca a mesa e mostra há quanto tempo a conta foi pedida', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        fetchOk({
          sessions: [
            sessionFixture({
              status: 'BILL_REQUESTED',
              billRequestedAt: '2026-08-02T18:44:00.000Z',
              waitingSeconds: 180,
            }),
          ],
          summary: { openSessions: 1, totalOpen: '186.40' },
        }),
      ),
    );

    render(<CashPanelPage identity={identity} onOpenBilling={vi.fn()} />);

    expect(await screen.findByText('há 3 min')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /dividir a conta/i })).toBeInTheDocument();
  });

  it('aciona onOpenBilling ao clicar em "Dividir a conta"', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        fetchOk({
          sessions: [sessionFixture({ status: 'BILL_REQUESTED', waitingSeconds: 30 })],
          summary: { openSessions: 1, totalOpen: '186.40' },
        }),
      ),
    );
    const onOpenBilling = vi.fn();

    render(<CashPanelPage identity={identity} onOpenBilling={onOpenBilling} />);
    const button = await screen.findByRole('button', { name: /dividir a conta/i });

    act(() => {
      button.click();
    });

    expect(onOpenBilling).toHaveBeenCalledWith('0198aabb-1111-7000-8000-000000000001');
  });

  it('busca por mesa/comanda com foco automático e debounce (US-050 §10)', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const fetchMock = vi.fn().mockResolvedValue(
      fetchOk({ sessions: [sessionFixture()], summary: { openSessions: 1, totalOpen: '186.40' } }),
    );
    vi.stubGlobal('fetch', fetchMock);

    render(<CashPanelPage identity={identity} />);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));

    const searchInput = screen.getByRole('searchbox', { name: /buscar por mesa ou comanda/i });
    expect(searchInput).toHaveFocus();

    fireEvent.change(searchInput, { target: { value: 'A47' } });

    // Antes do debounce vencer, nenhuma nova chamada.
    await act(async () => {
      vi.advanceTimersByTime(200);
    });
    expect(fetchMock).toHaveBeenCalledTimes(1);

    await act(async () => {
      vi.advanceTimersByTime(200);
    });

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    const [url] = fetchMock.mock.calls[1] as [string];
    expect(url).toContain('q=A47');
  });

  it('mostra estado vazio específico quando a busca não encontra nada', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(fetchOk({ sessions: [], summary: { openSessions: 0, totalOpen: '0.00' } })),
    );

    render(<CashPanelPage identity={identity} />);

    await waitFor(() => expect(screen.getByText('Nenhuma mesa aberta no momento')).toBeInTheDocument());
  });

  it('refaz a consulta quando o cliente realtime recebe table.changed', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(fetchOk({ sessions: [sessionFixture()], summary: { openSessions: 1, totalOpen: '186.40' } }))
      .mockResolvedValueOnce(
        fetchOk({
          sessions: [sessionFixture({ table: '99' })],
          summary: { openSessions: 1, totalOpen: '186.40' },
        }),
      );
    vi.stubGlobal('fetch', fetchMock);

    render(<CashPanelPage identity={identity} />);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));

    act(() => {
      capturedOptions?.onTableChanged({});
    });

    await waitFor(() => expect(screen.getByText('99')).toBeInTheDocument());
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('mostra mensagem de erro e mantém o último painel conhecido quando a rede cai (US-050 §9)', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(fetchOk({ sessions: [sessionFixture()], summary: { openSessions: 1, totalOpen: '186.40' } }))
      .mockRejectedValueOnce(new Error('network down'));
    vi.stubGlobal('fetch', fetchMock);

    render(<CashPanelPage identity={identity} />);
    await waitFor(() => expect(screen.getByText('12')).toBeInTheDocument());

    act(() => {
      capturedOptions?.onTableChanged({});
    });

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    // continua mostrando a sessão da última carga bem-sucedida, não uma tela em branco
    expect(screen.getByText('12')).toBeInTheDocument();
  });

  it('indica modo degradado (polling) quando o cliente realtime cai', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(fetchOk({ sessions: [sessionFixture()], summary: { openSessions: 1, totalOpen: '186.40' } })),
    );

    render(<CashPanelPage identity={identity} />);
    await waitFor(() => expect(screen.getByText('12')).toBeInTheDocument());
    expect(screen.getByText('Sincronizado')).toBeInTheDocument();

    act(() => {
      capturedOptions?.onModeChange?.('polling');
    });

    await waitFor(() => expect(screen.getByText('Sync atrasada')).toBeInTheDocument());
  });
});
