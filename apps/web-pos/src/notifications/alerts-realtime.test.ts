import { describe, expect, it, vi } from 'vitest';
import { AlertsRealtimeClient, type AlertsHubConnection } from './alerts-realtime.js';

function createFakeConnection() {
  const handlers = new Map<string, (payload: unknown) => void>();
  const connection: AlertsHubConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn((method: string, callback: (payload: unknown) => void) => {
      handlers.set(method, callback);
    }),
  };
  return { connection, emit: (method: string, payload: unknown) => handlers.get(method)?.(payload) };
}

describe('AlertsRealtimeClient (US-025/US-026)', () => {
  it('invoca onAlert quando o hub emite alert', async () => {
    const { connection, emit } = createFakeConnection();
    const onAlert = vi.fn();
    const client = new AlertsRealtimeClient(connection, { onAlert });

    await client.start();
    emit('alert', { type: 'table.waiter_called', data: { tableId: 'mesa-1', label: '12' } });

    expect(onAlert).toHaveBeenCalledWith({ type: 'table.waiter_called', data: { tableId: 'mesa-1', label: '12' } });
  });

  it('não lança quando a conexão falha — é reforço, não a única via do sinal', async () => {
    const { connection } = createFakeConnection();
    (connection.start as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('offline'));
    const client = new AlertsRealtimeClient(connection, { onAlert: vi.fn() });

    await expect(client.start()).resolves.toBeUndefined();
  });

  it('stop() encerra a conexão', async () => {
    const { connection } = createFakeConnection();
    const client = new AlertsRealtimeClient(connection, { onAlert: vi.fn() });

    await client.stop();

    expect(connection.stop).toHaveBeenCalledOnce();
  });
});
