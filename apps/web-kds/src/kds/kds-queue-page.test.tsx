// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
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
  // US-045 — sound-preferences.tsx (importado transitivamente por KdsQueuePage) também usa estas
  // três; este teste não verifica comportamento de som/preferência (coberto em
  // sound-preferences.test.tsx), só precisa que a importação não quebre.
  configureAlertSound: vi.fn(),
  playLateAlertChime: vi.fn(),
  previewAlertTone: vi.fn(),
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
    orderId: '0198aabb-1111-7000-8000-0000000000a1',
    orderCode: 'A47',
    productId: '0198aabb-1111-7000-8000-0000000000c1',
    productName: 'Pizza Calabresa Grande',
    quantity: 1,
    modifiers: ['sem cebola'],
    notes: 'bem assada',
    status: 'QUEUED',
    placedAt: new Date().toISOString(),
    elapsedSeconds: 10,
    thresholdState: 'NORMAL',
    warnSeconds: 720,
    criticalSeconds: 1080,
    table: '12',
    channel: 'DineIn',
    fractions: [],
    ...overrides,
  };
}

function queueResponse(items: readonly unknown[], lastEventId = 'x') {
  return { ok: true, json: () => Promise.resolve({ items, lastEventId }) };
}

/** US-042 — `useStationFilter` chama `GET /v1/catalog/stations` em paralelo à fila; testes que não
 * exercitam o filtro respondem `{ items: [] }` a essa chamada para manter `mode === 'single'`. */
function jsonOk(body: unknown) {
  return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
}

describe('KdsQueuePage (US-040/US-041)', () => {
  beforeEach(() => {
    capturedOptions = null;
    startMock.mockClear();
    stopMock.mockClear();
    vibrateAlertMock.mockClear();
    playAlertChimeMock.mockClear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    cleanup();
  });

  it('mostra mensagem clara quando o terminal não tem praça associada', async () => {
    render(<KdsQueuePage identity={{ ...identity, accessToken: tokenWithStation(null) }} />);

    expect(
      await screen.findByText(/não está associado a nenhuma praça de produção/i),
    ).toBeInTheDocument();
  });

  it('carrega e exibe um cartão por pedido da própria praça', async () => {
    const fetchMock = vi.fn().mockResolvedValue(queueResponse([ticketFixture()]));
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsQueuePage identity={identity} />);

    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());
    // "Pizza Calabresa Grande" também aparece no painel all-day (US-043) — escopa ao cartão.
    expect(within(screen.getByTestId('kds-ticket')).getByText('Pizza Calabresa Grande')).toBeInTheDocument();
    expect(screen.getByText('Mesa 12')).toBeInTheDocument();

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).toContain(`stationId=${STATION_ID}`);
  });

  it('agrupa dois itens do MESMO pedido em um único cartão (US-040 §3)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        queueResponse([
          ticketFixture({ orderItemId: '0198aabb-1111-7000-8000-000000000001', productName: 'Pizza Calabresa' }),
          ticketFixture({ orderItemId: '0198aabb-1111-7000-8000-000000000002', productName: 'Refrigerante' }),
        ]),
      ),
    );

    render(<KdsQueuePage identity={identity} />);

    await waitFor(() => expect(screen.getAllByTestId('kds-ticket')).toHaveLength(1));
    // Os dois nomes também aparecem no painel all-day (US-043) — escopa ao cartão.
    const ticket = within(screen.getByTestId('kds-ticket'));
    expect(ticket.getByText('Pizza Calabresa')).toBeInTheDocument();
    expect(ticket.getByText('Refrigerante')).toBeInTheDocument();
  });

  it('meio a meio combina o nome-base com os sabores das frações no cartão (US-040 §4)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        queueResponse([
          ticketFixture({
            productName: 'Pizza G',
            fractions: [
              { productName: 'Mussarela', weight: '0.5' },
              { productName: 'Calabresa', weight: '0.5' },
            ],
          }),
        ]),
      ),
    );

    render(<KdsQueuePage identity={identity} />);

    await waitFor(() => expect(screen.getByText('Pizza G · Mussarela / Calabresa')).toBeInTheDocument());
  });

  it('distingue visualmente o canal delivery do salão (critério de aceite)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        queueResponse([
          ticketFixture({
            orderItemId: '0198aabb-1111-7000-8000-000000000002',
            orderId: '0198aabb-1111-7000-8000-0000000000a2',
            orderCode: 'A48',
            channel: 'DineIn',
            table: '5',
          }),
          ticketFixture({
            orderItemId: '0198aabb-1111-7000-8000-000000000003',
            orderId: '0198aabb-1111-7000-8000-0000000000a3',
            orderCode: 'A49',
            channel: 'Delivery',
            table: null,
          }),
        ]),
      ),
    );

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getAllByTestId('kds-ticket')).toHaveLength(2));

    const tickets = screen.getAllByTestId('kds-ticket');
    const dineInTicket = tickets.find((el) => el.getAttribute('data-channel') === 'DineIn');
    const deliveryTicket = tickets.find((el) => el.getAttribute('data-channel') === 'Delivery');

    expect(dineInTicket).toBeDefined();
    expect(deliveryTicket).toBeDefined();
    // "Delivery" aparece duas vezes no ticket de entrega (rodapé "onde" + selo de canal) — a
    // própria duplicidade É a prova de distinção visual redundante (texto + ícone + borda de cor).
    expect(screen.getAllByText('Delivery').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Salão')).toBeInTheDocument();
  });

  it('refaz a consulta quando o cliente realtime recebe um kdsEvent', async () => {
    let queueCallCount = 0;
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/v1/catalog/stations')) return jsonOk({ items: [] });
      queueCallCount += 1;
      if (queueCallCount === 1) return jsonOk({ items: [ticketFixture()], lastEventId: '2026-08-03T12:00:00.000Z' });
      return jsonOk({
        items: [
          ticketFixture({
            orderItemId: '0198aabb-1111-7000-8000-000000000009',
            orderId: '0198aabb-1111-7000-8000-0000000000b9',
            orderCode: 'B12',
          }),
        ],
        lastEventId: '2026-08-03T12:00:05.000Z',
      });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(queueCallCount).toBe(1));

    act(() => {
      capturedOptions?.onEvent({ type: 'order.item.queued', data: {} });
    });

    await waitFor(() => expect(screen.getByText('B12')).toBeInTheDocument());
    expect(queueCallCount).toBe(2);
    // Cenário Gherkin "Chegada ao KDS": item novo dispara som/vibração.
    expect(vibrateAlertMock).toHaveBeenCalledOnce();
    expect(playAlertChimeMock).toHaveBeenCalledOnce();
  });

  it('não soa alerta na primeira carga (só em chegada NOVA depois da tela já estar de pé)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(queueResponse([ticketFixture()])));

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    expect(vibrateAlertMock).not.toHaveBeenCalled();
    expect(playAlertChimeMock).not.toHaveBeenCalled();
  });

  it('indica modo degradado (polling) quando o cliente realtime cai (ADR-011)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(queueResponse([ticketFixture()])));

    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());
    expect(screen.getByText('Sincronizado')).toBeInTheDocument();

    act(() => {
      capturedOptions?.onModeChange?.('polling');
    });

    await waitFor(() => expect(screen.getByText('Sync atrasada')).toBeInTheDocument());
  });

  it('mostra fila vazia com mensagem "cozinha em dia"', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(queueResponse([])));

    render(<KdsQueuePage identity={identity} />);

    await waitFor(() => expect(screen.getByText(/Cozinha em dia/)).toBeInTheDocument());
  });

  it('mantém a última fila conhecida e mostra erro quando a rede cai (US-031 §9)', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(queueResponse([ticketFixture()]))
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

describe('KdsQueuePage — teclado numérico (US-041)', () => {
  beforeEach(() => {
    capturedOptions = null;
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    cleanup();
  });

  function stubAdvanceFlow(options: {
    readonly advanceResponse?: { ok: boolean; status?: number; body: unknown };
    readonly undoResponse?: { ok: boolean; status?: number; body: unknown };
  } = {}) {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? 'GET';

      if (url.includes('/v1/kds/queue')) {
        return Promise.resolve(queueResponse([ticketFixture()]));
      }
      if (method === 'POST' && url.includes('/advance') && url.includes('/orders/')) {
        const response = options.advanceResponse ?? {
          ok: true,
          body: {
            advanced: [
              {
                id: '0198aabb-1111-7000-8000-000000000001',
                orderId: '0198aabb-1111-7000-8000-0000000000a1',
                variantId: '0198aabb-1111-7000-8000-000000000fff',
                name: 'Pizza Calabresa Grande',
                quantity: 1,
                unitPrice: '40.00',
                totalPrice: '40.00',
                status: 'FIRED',
                notes: null,
                stationId: STATION_ID,
                repeatedFromItemId: null,
              },
            ],
          },
        };
        return Promise.resolve({
          ok: response.ok,
          status: response.status ?? (response.ok ? 200 : 404),
          json: () => Promise.resolve(response.body),
        });
      }
      if (method === 'POST' && url.includes('/undo')) {
        const response = options.undoResponse ?? {
          ok: true,
          body: {
            id: '0198aabb-1111-7000-8000-000000000001',
            orderId: '0198aabb-1111-7000-8000-0000000000a1',
            variantId: '0198aabb-1111-7000-8000-000000000fff',
            name: 'Pizza Calabresa Grande',
            quantity: 1,
            unitPrice: '40.00',
            totalPrice: '40.00',
            status: 'QUEUED',
            notes: null,
            stationId: STATION_ID,
            repeatedFromItemId: null,
          },
        };
        return Promise.resolve({
          ok: response.ok,
          status: response.status ?? (response.ok ? 200 : 409),
          json: () => Promise.resolve(response.body),
        });
      }
      return Promise.reject(new Error(`unexpected fetch ${method} ${url}`));
    });
    vi.stubGlobal('fetch', fetchMock);
    return fetchMock;
  }

  it('digitar o código do pedido e Enter chama o avanço por código, sem lote', async () => {
    const fetchMock = stubAdvanceFlow();
    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: '4' }));
    fireEvent.click(screen.getByRole('button', { name: '7' }));
    fireEvent.click(screen.getByRole('button', { name: 'Enter' }));

    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([url]) => String(url).includes('/v1/kds/orders/47/advance'))).toBe(true),
    );
    const call = fetchMock.mock.calls.find(([url]) => String(url).includes('/v1/kds/orders/47/advance'))!;
    const init = call[1] as RequestInit;
    expect(JSON.parse(init.body as string)).toEqual({ stationId: STATION_ID, batch: false });
  });

  it('botão Lote envia batch=true', async () => {
    const fetchMock = stubAdvanceFlow();
    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: '4' }));
    fireEvent.click(screen.getByRole('button', { name: '7' }));
    fireEvent.click(screen.getByRole('button', { name: 'Lote' }));

    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([url]) => String(url).includes('/v1/kds/orders/47/advance'))).toBe(true),
    );
    const call = fetchMock.mock.calls.find(([url]) => String(url).includes('/v1/kds/orders/47/advance'))!;
    const init = call[1] as RequestInit;
    expect(JSON.parse(init.body as string)).toEqual({ stationId: STATION_ID, batch: true });
  });

  it('código inexistente mostra erro no teclado sem travar a tela (US-041 §10)', async () => {
    stubAdvanceFlow({
      advanceResponse: { ok: false, status: 404, body: { detail: 'Nenhum pedido encontrado.', code: 'SHORT_CODE_NOT_FOUND' } },
    });
    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: 'Enter' }));

    await waitFor(() => expect(screen.getByTestId('kds-keypad-error')).toHaveTextContent('Nenhum pedido encontrado.'));
    expect(screen.getByText('A47')).toBeInTheDocument();
  });

  it('avanço bem-sucedido habilita o desfazer, e desfazer chama o undo do item', async () => {
    const fetchMock = stubAdvanceFlow();
    render(<KdsQueuePage identity={identity} />);
    await waitFor(() => expect(screen.getByText('A47')).toBeInTheDocument());

    expect(screen.getByTestId('kds-keypad-undo')).toBeDisabled();

    fireEvent.click(screen.getByRole('button', { name: '4' }));
    fireEvent.click(screen.getByRole('button', { name: '7' }));
    fireEvent.click(screen.getByRole('button', { name: 'Enter' }));

    await waitFor(() => expect(screen.getByTestId('kds-keypad-undo')).toBeEnabled());

    fireEvent.click(screen.getByTestId('kds-keypad-undo'));

    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([url]) => String(url).includes('/v1/kds/items/0198aabb-1111-7000-8000-000000000001/undo'))).toBe(true),
    );
    await waitFor(() => expect(screen.getByTestId('kds-keypad-undo')).toBeDisabled());
  });
});
