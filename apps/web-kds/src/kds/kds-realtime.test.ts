import { describe, expect, it, vi } from 'vitest';
import { KdsRealtimeClient, type KdsHubConnection } from './kds-realtime.js';

/** Duplo de teste de `HubConnection` — captura os callbacks registrados sem abrir socket nenhum. */
function createFakeConnection() {
  const handlers = new Map<string, (payload: unknown) => void>();
  let closeHandler: ((error?: Error) => void) | undefined;
  let reconnectingHandler: ((error?: Error) => void) | undefined;
  let reconnectedHandler: ((connectionId?: string) => void) | undefined;

  const connection: KdsHubConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
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

describe('KdsRealtimeClient (US-031, ADR-011)', () => {
  it('invoca onEvent quando o hub emite kdsEvent', async () => {
    const { connection, emit } = createFakeConnection();
    const onEvent = vi.fn();
    const client = new KdsRealtimeClient(connection, { onEvent, poll: vi.fn(), getLastEventId: () => undefined });

    await client.start();
    emit('kdsEvent', { type: 'order.placed', data: {} });

    expect(onEvent).toHaveBeenCalledWith({ type: 'order.placed', data: {} });
  });

  it('chama Resume com o lastEventId corrente ao conectar (ADR-011)', async () => {
    const { connection } = createFakeConnection();
    const client = new KdsRealtimeClient(connection, {
      onEvent: vi.fn(),
      poll: vi.fn(),
      getLastEventId: () => '2026-08-03T12:00:00.000Z',
    });

    await client.start();

    expect(connection.invoke).toHaveBeenCalledWith('Resume', '2026-08-03T12:00:00.000Z');
  });

  it('entra em modo polling assim que a conexão cai e sonda a cada 5s (ADR-011)', () => {
    vi.useFakeTimers();
    try {
      const { connection, simulateClose } = createFakeConnection();
      const poll = vi.fn();
      const onModeChange = vi.fn();
      const client = new KdsRealtimeClient(connection, {
        onEvent: vi.fn(),
        onModeChange,
        poll,
        getLastEventId: () => undefined,
      });

      simulateClose();

      expect(client.currentMode).toBe('polling');
      expect(onModeChange).toHaveBeenCalledWith('polling');
      // Sonda imediatamente ao cair — não espera o primeiro tick de 5s (cenário Gherkin "Queda do
      // WebSocket no KDS": "em no máximo 5 segundos").
      expect(poll).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(5000);
      expect(poll).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('ao reconectar, chama Resume de novo e volta para "ws" (cenário Gherkin "Reconexão com recuperação")', async () => {
    vi.useFakeTimers();
    try {
      const { connection, simulateClose, simulateReconnected } = createFakeConnection();
      const onModeChange = vi.fn();
      let lastEventId = '2026-08-03T12:00:00.000Z';
      const client = new KdsRealtimeClient(connection, {
        onEvent: vi.fn(),
        onModeChange,
        poll: vi.fn(),
        getLastEventId: () => lastEventId,
      });

      simulateClose();
      expect(client.currentMode).toBe('polling');

      lastEventId = '2026-08-03T12:00:40.000Z'; // 40s desconectado (cenário Gherkin)
      simulateReconnected();
      await vi.waitFor(() => {
        expect(connection.invoke).toHaveBeenCalledWith('Resume', '2026-08-03T12:00:40.000Z');
      });
      expect(client.currentMode).toBe('ws');
      expect(onModeChange).toHaveBeenCalledWith('ws');
    } finally {
      vi.useRealTimers();
    }
  });

  it('não deixa o polling em pé quando stop() é chamado', () => {
    vi.useFakeTimers();
    try {
      const { connection, simulateClose } = createFakeConnection();
      const poll = vi.fn();
      const client = new KdsRealtimeClient(connection, { onEvent: vi.fn(), poll, getLastEventId: () => undefined });

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

  it('cai para polling quando a primeira tentativa de conexão falha', async () => {
    const { connection } = createFakeConnection();
    (connection.start as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('offline'));
    const poll = vi.fn();
    const client = new KdsRealtimeClient(connection, { onEvent: vi.fn(), poll, getLastEventId: () => undefined });

    await client.start();

    expect(client.currentMode).toBe('polling');
    expect(poll).toHaveBeenCalledTimes(1);
  });

  it('nunca lança quando Resume falha — silencioso, o polling de fallback cobre', async () => {
    const { connection } = createFakeConnection();
    (connection.invoke as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('hub não respondeu'));
    const client = new KdsRealtimeClient(connection, { onEvent: vi.fn(), poll: vi.fn(), getLastEventId: () => undefined });

    await expect(client.start()).resolves.toBeUndefined();
  });
});
