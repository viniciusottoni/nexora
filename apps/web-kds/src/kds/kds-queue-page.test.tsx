// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { KdsQueuePage } from './kds-queue-page.js';

// A conexão SignalR de verdade abriria um socket de rede — fora do escopo de um teste de
// componente (isso já é coberto, sem rede nenhuma, por kds-realtime.test.ts). Aqui só precisamos
// confirmar que a tela reage ao que o cliente realtime relata (onModeChange/onEvent/poll), mesmo
// padrão de apps/web-pos/src/table-map/table-map-page.test.tsx.
const startMock = vi.fn().mockResolvedValue(undefined);
const stopMock = vi.fn().mockResolvedValue(undefined);
let capturedOptions: {
  onEvent: (event: unknown) => void;
  onModeChange?: (mode: 'ws' | 'polling') => void;
  poll: () => void;
  getLastEventId: () => string | undefined;
} | null = null;

vi.mock('./kds-realtime.js', () => ({
  createKdsHubConnection: vi.fn(() => ({})),
  KdsRealtimeClient: vi.fn().mockImplementation((_connection: unknown, options: typeof capturedOptions) => {
    capturedOptions = options;
    return { start: startMock, stop: stopMock, currentMode: 'ws' as const };
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

// stn=0198aabb-1111-7000-8000-000000000050 (payload de um dispositivo pareado à praça "forno").
const STATION_ID = '0198aabb-1111-7000-8000-000000000050';
function tokenWithStation(stationId: string | null): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  const payload: Record<string, unknown> = { sub: 'user-1' };
  if (stationId) payload.stn = stationId;
  return `${encode({ alg: 'HS256' })}.${encode(payload)}.sig`;
}

const identity = {
  accessToken: tokenWithStation(STATION_ID),
  deviceId: 'device-1',
  deviceSecret: 'secret-1',
};

function ticketFixture(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    orderItemId: '0198aabb-1111-7000-8000-000000000001',
    orderCode: 'A47',
    productName: 'Pizza Calabresa Grande',
    quantity: 1,
    modifiers: ['sem cebola'],
    notes: 'bem assada',
    status: 'QUEUED',
    placedAt: new Date().toISOString(),
    elapsedSeconds: 10,
    table: '12',
    channel: 'DineIn',
    ...overrides,
  };
}

describe('KdsQueuePage (US-031)', () => {
  beforeEach(() => {
    capturedOptions = null;
    startMock.mockClear();
    stopMock.mockClear();
    vibrateAlertMock.mockClear();
    playAlertChimeMock.mockClear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('mostra mensagem clara quando o terminal não tem praça associada', async () => {
    render(<KdsQueuePage identity={{ ...identity, accessToken: tokenWithStation(null) }} />);

    expect(
      await screen.findByText(/não está associado a nenhuma praça de produção/i),
    ).toBeInTheDocument();
  });

  it('carrega e exibe os tickets da fila da própria praça', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: () =>
        Promise.resolve({
          items: [ticketFixture()],
          lastEventId: '2026-08-03T12:00:00.000Z',
        }),
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsQueuePage identity={identity} />);

    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());
    expect(screen.getByText('Pizza Calabresa Grande')).toBeInTheDocument();
    expect(screen.getByText('Mesa 12')).toBeInTheDocument();

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).toContain(`stationId=${STATION_ID}`);
  });

  it('distingue visualmente o canal delivery do salão (critério de aceite)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: () =>
          Promise.resolve({
            items: [
              ticketFixture({ orderItemId: '0198aabb-1111-7000-8000-000000000002', channel: 'DineIn', table: '5' }),
              ticketFixture({
                orderItemId: '0198aabb-1111-7000-8000-000000000003',
                channel: 'Delivery',
                table: null,
              }),
            ],
            lastEventId: '2026-08-03T12:00:00.000Z',
          }),
      }),
    );

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getAllByTestId('kds-ticket')).toHaveLength(2));

    const tickets = screen.getAllByTestId('kds-ticket');
    const dineInTicket = tickets.find((el) => el.getAttribute('data-channel') === 'DineIn');
    const deliveryTicket = tickets.find((el) => el.getAttribute('data-channel') === 'Delivery');

    expect(dineInTicket).toBeDefined();
    expect(deliveryTicket).toBeDefined();
    expect(dineInTicket?.getAttribute('data-channel')).not.toBe(deliveryTicket?.getAttribute('data-channel'));
    // "Delivery" aparece duas vezes no ticket de entrega (rodapé "onde" + selo de canal) — a
    // própria duplicidade É a prova de distinção visual redundante (texto + ícone + borda de cor).
    expect(screen.getAllByText('Delivery').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Salão')).toBeInTheDocument();
  });

  it('refaz a consulta quando o cliente realtime recebe um kdsEvent', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ items: [ticketFixture()], lastEventId: '2026-08-03T12:00:00.000Z' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            items: [ticketFixture({ orderItemId: '0198aabb-1111-7000-8000-000000000009', orderCode: 'B12' })],
            lastEventId: '2026-08-03T12:00:05.000Z',
          }),
      });
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));

    act(() => {
      capturedOptions?.onEvent({ type: 'order.item.queued', data: {} });
    });

    await waitFor(() => expect(screen.getByText('B12')).toBeInTheDocument());
    expect(fetchMock).toHaveBeenCalledTimes(2);
    // Cenário Gherkin "Chegada ao KDS": item novo dispara som/vibração.
    expect(vibrateAlertMock).toHaveBeenCalledOnce();
    expect(playAlertChimeMock).toHaveBeenCalledOnce();
  });

  it('não soa alerta na primeira carga (só em chegada NOVA depois da tela já estar de pé)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ items: [ticketFixture()], lastEventId: 'x' }) }),
    );

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    expect(vibrateAlertMock).not.toHaveBeenCalled();
    expect(playAlertChimeMock).not.toHaveBeenCalled();
  });

  it('indica modo degradado (polling) quando o cliente realtime cai (ADR-011)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ items: [ticketFixture()], lastEventId: 'x' }) }),
    );

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());
    expect(screen.getByText('Sincronizado')).toBeInTheDocument();

    act(() => {
      capturedOptions?.onModeChange?.('polling');
    });

    await waitFor(() => expect(screen.getByText('Sync atrasada')).toBeInTheDocument());
  });

  it('mostra fila vazia com mensagem "cozinha em dia"', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ items: [], lastEventId: 'x' }) }),
    );

    render(<KdsQueuePage identity={identity} />);

    await waitFor(() => expect(screen.getByText(/Cozinha em dia/)).toBeInTheDocument());
  });

  it('mantém a última fila conhecida e mostra erro quando a rede cai (US-031 §9)', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve({ items: [ticketFixture()], lastEventId: 'x' }) })
      .mockRejectedValueOnce(new Error('network down'));
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    act(() => {
      capturedOptions?.onEvent({ type: 'order.item.queued', data: {} });
    });

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    expect(screen.getByText('A47')).toBeInTheDocument();
  });
});
