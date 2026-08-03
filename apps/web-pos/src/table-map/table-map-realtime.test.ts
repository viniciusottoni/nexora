import { describe, expect, it, vi } from 'vitest';
import { TableMapRealtimeClient, type TableMapHubConnection } from './table-map-realtime.js';

/** Duplo de teste de `HubConnection` — captura os callbacks registrados sem abrir socket nenhum. */
function createFakeConnection() {
  const handlers = new Map<string, (payload: unknown) => void>();
  let closeHandler: ((error?: Error) => void) | undefined;
  let reconnectingHandler: ((error?: Error) => void) | undefined;
  let reconnectedHandler: ((connectionId?: string) => void) | undefined;

  const connection: TableMapHubConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn((method: string, callback: (payload: unknown) => void) => {
      handlers.set(method, callback);
    }),
    onclose: vi.fn((callback) => {
      closeHandler = callback;
    }),
    onreconnecting: vi.fn((callback) => {
      reconnectingHandler = callback;
    }),
    onreconnected: vi.fn((callback) => {
      reconnectedHandler = callback;
    }),
  };

  return {
    connection,
    emit: (method: string, payload: unknown) => handlers.get(method)?.(payload),
    simulateClose: () => closeHandler?.(),
    simulateReconnecting: () => reconnectingHandler?.(),
    simulateReconnected: () => reconnectedHandler?.(),
  };
}

describe('TableMapRealtimeClient (ADR-011)', () => {
  it('invoca onTableChanged quando o hub emite table.changed', async () => {
    const { connection, emit } = createFakeConnection();
    const onTableChanged = vi.fn();
    const client = new TableMapRealtimeClient(connection, { onTableChanged, poll: vi.fn() });

    await client.start();
    emit('table.changed', { id: 'mesa-1' });

    expect(onTableChanged).toHaveBeenCalledWith({ id: 'mesa-1' });
  });

  it('entra em modo polling assim que a conexão cai e sonda a cada 5s (ADR-011)', () => {
    vi.useFakeTimers();
    try {
      const { connection, simulateClose } = createFakeConnection();
      const poll = vi.fn();
      const onModeChange = vi.fn();
      const client = new TableMapRealtimeClient(connection, { onTableChanged: vi.fn(), onModeChange, poll });

      simulateClose();

      expect(client.currentMode).toBe('polling');
      expect(onModeChange).toHaveBeenCalledWith('polling');
      // Sonda imediatamente ao cair — não espera o primeiro tick de 5s (US-023 §4, "em até 5 segundos").
      expect(poll).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(5000);
      expect(poll).toHaveBeenCalledTimes(2);

      vi.advanceTimersByTime(5000);
      expect(poll).toHaveBeenCalledTimes(3);
    } finally {
      vi.useRealTimers();
    }
  });

  it('para de sondar e volta para "ws" quando reconecta', () => {
    vi.useFakeTimers();
    try {
      const { connection, simulateClose, simulateReconnected } = createFakeConnection();
      const poll = vi.fn();
      const onModeChange = vi.fn();
      const client = new TableMapRealtimeClient(connection, { onTableChanged: vi.fn(), onModeChange, poll });

      simulateClose();
      expect(client.currentMode).toBe('polling');

      simulateReconnected();
      expect(client.currentMode).toBe('ws');
      expect(onModeChange).toHaveBeenCalledWith('ws');

      poll.mockClear();
      vi.advanceTimersByTime(20000);
      expect(poll).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });

  it('stop() encerra o polling em andamento', () => {
    vi.useFakeTimers();
    try {
      const { connection, simulateClose } = createFakeConnection();
      const poll = vi.fn();
      const client = new TableMapRealtimeClient(connection, { onTableChanged: vi.fn(), poll });

      simulateClose();
      void client.stop();

      poll.mockClear();
      vi.advanceTimersByTime(30000);
      expect(poll).not.toHaveBeenCalled();
      expect(connection.stop).toHaveBeenCalledOnce();
    } finally {
      vi.useRealTimers();
    }
  });

  it('cai para polling quando a primeira tentativa de conexão falha (WebSocket indisponível de saída)', async () => {
    const { connection } = createFakeConnection();
    (connection.start as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('offline'));
    const poll = vi.fn();
    const client = new TableMapRealtimeClient(connection, { onTableChanged: vi.fn(), poll });

    await client.start();

    expect(client.currentMode).toBe('polling');
    expect(poll).toHaveBeenCalledTimes(1);
  });
});
