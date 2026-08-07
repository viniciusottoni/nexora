// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TableMapPage } from './table-map-page.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

// A conexão SignalR de verdade abriria um socket de rede — fora do escopo de um teste de
// componente (isso já é coberto, sem rede nenhuma, por table-map-realtime.test.ts). Aqui só
// precisamos confirmar que a tela invoca a fábrica com a URL certa e reage ao que o cliente
// realtime relata (onModeChange/onTableChanged/poll).
const startMock = vi.fn().mockResolvedValue(undefined);
const stopMock = vi.fn().mockResolvedValue(undefined);
let capturedOptions: {
  onTableChanged: (table: unknown) => void;
  onModeChange?: (mode: 'ws' | 'polling') => void;
  poll: () => void;
} | null = null;

vi.mock('./table-map-realtime.js', () => ({
  createTableMapHubConnection: vi.fn(() => ({})),
  TableMapRealtimeClient: vi.fn().mockImplementation((_connection: unknown, options: typeof capturedOptions) => {
    capturedOptions = options;
    return { start: startMock, stop: stopMock, currentMode: 'ws' as const };
  }),
}));

// US-025 §10: mesmo motivo do mock acima — a conexão real do AlertsHub também abriria um socket
// de rede (coberto sem rede nenhuma por alerts-realtime.test.ts). Aqui só precisamos confirmar
// que a tela não quebra ao montar/desmontar este segundo canal e reage ao alerta recebido.
let capturedAlertOptions: { onAlert: (alert: { type: string; data: Record<string, unknown> }) => void } | null = null;

vi.mock('../notifications/alerts-realtime.js', () => ({
  createAlertsHubConnection: vi.fn(() => ({})),
  AlertsRealtimeClient: vi.fn().mockImplementation((_connection: unknown, options: typeof capturedAlertOptions) => {
    capturedAlertOptions = options;
    return {
      start: vi.fn().mockResolvedValue(undefined),
      stop: vi.fn().mockResolvedValue(undefined),
    };
  }),
}));

const vibrateAlertMock = vi.fn();
const playAlertChimeMock = vi.fn();

vi.mock('../notifications/alert-sound.js', () => ({
  vibrateAlert: (...args: unknown[]) => {
    vibrateAlertMock(...args);
  },
  playAlertChime: (...args: unknown[]) => {
    playAlertChimeMock(...args);
  },
}));

function tableFixture(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: '0198aabb-1111-7000-8000-000000000001',
    label: '12',
    area: 'Salão',
    status: 'OCCUPIED',
    seats: 4,
    session: {
      openedAt: '2026-08-02T18:00:00.000Z',
      minutesOpen: 47,
      total: '186.40',
      guestCount: 4,
      waiter: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Ana' },
      sessionId: '0198aabb-1111-7000-8000-000000000008',
    },
    flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false },
    ...overrides,
  };
}

describe('TableMapPage', () => {
  beforeEach(() => {
    capturedOptions = null;
    capturedAlertOptions = null;
    startMock.mockClear();
    stopMock.mockClear();
    vibrateAlertMock.mockClear();
    playAlertChimeMock.mockClear();
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('carrega e agrupa as mesas por ambiente', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: () =>
        Promise.resolve({
          tables: [
            tableFixture({ id: '0198aabb-1111-7000-8000-000000000010', label: '1', area: 'Salão' }),
            tableFixture({
              id: '0198aabb-1111-7000-8000-000000000011',
              label: '5',
              area: 'Varanda',
              status: 'FREE',
              session: null,
            }),
          ],
        }),
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<TableMapPage identity={identity} />);

    await waitFor(() => expect(screen.getByRole('button', { name: /^Mesa 1 / })).toBeInTheDocument());
    expect(screen.getByRole('heading', { name: 'Salão' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Varanda' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Mesa 5/ })).toBeInTheDocument();
  });

  it('US-025 §7: confirma o atendimento a partir do mapa e recarrega', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ tables: [tableFixture({ flags: { waiterCalled: true, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false } })] }),
      })
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve({ acknowledged: true, resolved: true, responseSeconds: 9 }) })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ tables: [tableFixture({ flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false } })] }),
      });
    vi.stubGlobal('fetch', fetchMock);

    render(<TableMapPage identity={identity} />);
    const acknowledgeButton = await screen.findByRole('button', { name: /atendido/i });

    await act(async () => {
      acknowledgeButton.click();
    });

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    const [ackUrl, ackInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(ackUrl).toContain('/acknowledge-call');
    expect(ackInit.method).toBe('POST');
  });

  it('US-026 §4: pede a conta a partir de uma mesa ocupada e recarrega', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve({ tables: [tableFixture()] }) })
      .mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            session: {
              id: '0198aabb-1111-7000-8000-000000000009',
              tableId: '0198aabb-1111-7000-8000-000000000001',
              tableLabel: '12',
              status: 'BILLREQUESTED',
              openedAt: new Date().toISOString(),
              guestCount: 1,
              guestCountConfirmed: true,
              waiterId: null,
              source: 'WAITER',
              currentItems: [],
              total: '0.00',
            },
            alreadyRequested: false,
          }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({ tables: [tableFixture({ flags: { waiterCalled: false, billRequested: true, itemsReadyToServe: 0, aboveAvgDuration: false } })] }),
      });
    vi.stubGlobal('fetch', fetchMock);

    render(<TableMapPage identity={identity} />);
    const requestBillButton = await screen.findByRole('button', { name: /pedir conta/i });

    await act(async () => {
      requestBillButton.click();
    });

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    const [billUrl] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(billUrl).toContain('/request-bill');
  });

  it('US-025 §10: alerta table.waiter_called dispara vibração/som e mostra o aviso', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ tables: [tableFixture()] }) }),
    );

    render(<TableMapPage identity={identity} />);
    await waitFor(() => expect(capturedAlertOptions).not.toBeNull());

    act(() => {
      capturedAlertOptions?.onAlert({ type: 'table.waiter_called', data: { tableId: 'mesa-1', label: '7' } });
    });

    expect(vibrateAlertMock).toHaveBeenCalledOnce();
    expect(playAlertChimeMock).toHaveBeenCalledOnce();
    expect(await screen.findByText('Mesa 7 está chamando você!')).toBeInTheDocument();
  });

  it('ignora tipos de alerta que não são table.waiter_called', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ tables: [tableFixture()] }) }),
    );

    render(<TableMapPage identity={identity} />);
    await waitFor(() => expect(capturedAlertOptions).not.toBeNull());

    act(() => {
      capturedAlertOptions?.onAlert({ type: 'table.bill_requested', data: { tableId: 'mesa-1' } });
    });

    expect(vibrateAlertMock).not.toHaveBeenCalled();
    expect(playAlertChimeMock).not.toHaveBeenCalled();
  });

  it('mostra estado vazio quando não há mesas', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ tables: [] }) }),
    );

    render(<TableMapPage identity={identity} />);

    await waitFor(() => expect(screen.getByText('Nenhuma mesa cadastrada.')).toBeInTheDocument());
  });

  it('refaz a consulta quando o cliente realtime recebe table.changed', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve({ tables: [tableFixture()] }) })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ tables: [tableFixture({ label: '99' })] }),
      });
    vi.stubGlobal('fetch', fetchMock);

    render(<TableMapPage identity={identity} />);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));

    capturedOptions?.onTableChanged({});

    await waitFor(() => expect(screen.getByRole('button', { name: /Mesa 99/ })).toBeInTheDocument());
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('indica modo degradado (polling) quando o cliente realtime cai', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ tables: [tableFixture()] }) }),
    );

    render(<TableMapPage identity={identity} />);
    await waitFor(() => expect(screen.getByRole('button', { name: /Mesa 12/ })).toBeInTheDocument());

    expect(screen.getByText('Sincronizado')).toBeInTheDocument();

    act(() => {
      capturedOptions?.onModeChange?.('polling');
    });

    await waitFor(() => expect(screen.getByText('Sync atrasada')).toBeInTheDocument());
  });

  it('mostra mensagem de erro e mantém o último mapa conhecido quando a rede cai (US-023 §9)', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve({ tables: [tableFixture()] }) })
      .mockRejectedValueOnce(new Error('network down'));
    vi.stubGlobal('fetch', fetchMock);

    render(<TableMapPage identity={identity} />);
    await waitFor(() => expect(screen.getByRole('button', { name: /Mesa 12/ })).toBeInTheDocument());

    capturedOptions?.onTableChanged({});

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    // continua mostrando a mesa da última carga bem-sucedida, não uma tela em branco
    expect(screen.getByRole('button', { name: /Mesa 12/ })).toBeInTheDocument();
  });
});
