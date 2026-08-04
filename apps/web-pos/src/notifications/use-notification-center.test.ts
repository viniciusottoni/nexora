// @vitest-environment jsdom
import { act, renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { NotificationCenterApi } from './notification-center-api.js';
import { subscribeToPush } from './push-notifications.js';
import { useNotificationCenter } from './use-notification-center.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

const { fakeConnection, emitAlert } = vi.hoisted(() => {
  const handlers = new Map<string, (payload: unknown) => void>();
  const connection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn((method: string, callback: (payload: unknown) => void) => {
      handlers.set(method, callback);
    }),
  };
  return {
    fakeConnection: connection,
    emitAlert: (payload: unknown) => handlers.get('alert')?.(payload),
  };
});

vi.mock('./alerts-realtime.js', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./alerts-realtime.js')>();
  return { ...actual, createAlertsHubConnection: vi.fn(() => fakeConnection) };
});

vi.mock('./alert-sound.js', () => ({
  vibrateAlert: vi.fn(),
  playAlertChime: vi.fn(),
}));

vi.mock('./push-notifications.js', () => ({
  subscribeToPush: vi.fn().mockResolvedValue(true),
}));

function alertFixture(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: '0198aabb-1111-7000-8000-000000000001',
    type: 'ORDER_LATE',
    severity: 'HIGH',
    entityType: 'ORDER',
    entityId: '0198aabb-1111-7000-8000-000000000002',
    message: 'Pedido A47 da mesa 12 está há 21 minutos na fila.',
    raisedAt: '2026-08-04T12:00:00.000Z',
    acknowledgedAt: null,
    acknowledgedBy: null,
    resolvedAt: null,
    targetRoles: ['WAITER'],
    targetUserId: null,
    groupKey: null,
    ...overrides,
  };
}

class FakeNotification {
  static permission: NotificationPermission = 'default';
  static requestPermission = vi.fn(async () => FakeNotification.permission);
}

describe('useNotificationCenter (US-081/US-083)', () => {
  afterEach(() => {
    vi.clearAllMocks();
    vi.unstubAllGlobals();
    FakeNotification.permission = 'default';
  });

  it('carrega as notificações não lidas no boot', async () => {
    const api = { listUnread: vi.fn().mockResolvedValue({ alerts: [alertFixture()], nextCursor: null }) } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.items).toHaveLength(1);
    expect(api.listUnread).toHaveBeenCalledWith(identity);
  });

  it('fica ocioso (sem fetch) enquanto a identidade ainda não existe (pré-login)', async () => {
    const api = { listUnread: vi.fn() } as unknown as NotificationCenterApi;

    renderHook(() => useNotificationCenter({ identity: undefined, api }));

    await Promise.resolve();
    expect(api.listUnread).not.toHaveBeenCalled();
  });

  it('alert.raised: toca som/vibração e atualiza a lista', async () => {
    const { vibrateAlert, playAlertChime } = await import('./alert-sound.js');
    const api = {
      listUnread: vi
        .fn()
        .mockResolvedValueOnce({ alerts: [], nextCursor: null })
        .mockResolvedValueOnce({ alerts: [alertFixture()], nextCursor: null }),
    } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      emitAlert({ type: 'alert.raised', data: { alertId: alertFixture().id, count: null } });
    });

    await waitFor(() => expect(result.current.items).toHaveLength(1));
    expect(vibrateAlert).toHaveBeenCalledOnce();
    expect(playAlertChime).toHaveBeenCalledOnce();
  });

  it('alert.group_updated: atualiza a lista SEM tocar som (US-083 §10)', async () => {
    const { vibrateAlert, playAlertChime } = await import('./alert-sound.js');
    const api = {
      listUnread: vi
        .fn()
        .mockResolvedValueOnce({ alerts: [alertFixture({ message: '4 pedidos atrasados' })], nextCursor: null })
        .mockResolvedValueOnce({ alerts: [alertFixture({ message: '5 pedidos atrasados' })], nextCursor: null }),
    } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));
    await waitFor(() => expect(result.current.loading).toBe(false));
    vi.clearAllMocks();

    act(() => {
      emitAlert({ type: 'alert.group_updated', data: { alertId: alertFixture().id, count: 5 } });
    });

    await waitFor(() => expect(result.current.items[0]?.message).toBe('5 pedidos atrasados'));
    expect(vibrateAlert).not.toHaveBeenCalled();
    expect(playAlertChime).not.toHaveBeenCalled();
  });

  it('alert.resolved: atualiza a lista SEM tocar som', async () => {
    const { vibrateAlert, playAlertChime } = await import('./alert-sound.js');
    const api = {
      listUnread: vi
        .fn()
        .mockResolvedValueOnce({ alerts: [alertFixture()], nextCursor: null })
        .mockResolvedValueOnce({ alerts: [], nextCursor: null }),
    } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));
    await waitFor(() => expect(result.current.items).toHaveLength(1));
    vi.clearAllMocks();

    act(() => {
      emitAlert({ type: 'alert.resolved', data: { alertId: alertFixture().id } });
    });

    await waitFor(() => expect(result.current.items).toHaveLength(0));
    expect(vibrateAlert).not.toHaveBeenCalled();
    expect(playAlertChime).not.toHaveBeenCalled();
  });

  it('acknowledge: chama a API e mantém o item na lista marcado como lido', async () => {
    const acknowledged = alertFixture({ acknowledgedAt: '2026-08-04T12:05:00.000Z' });
    const api = {
      listUnread: vi.fn().mockResolvedValue({ alerts: [alertFixture()], nextCursor: null }),
      acknowledge: vi.fn().mockResolvedValue(acknowledged),
    } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));
    await waitFor(() => expect(result.current.items).toHaveLength(1));

    await act(async () => {
      await result.current.acknowledge(alertFixture().id);
    });

    expect(api.acknowledge).toHaveBeenCalledWith(identity, alertFixture().id);
    expect(result.current.items).toHaveLength(1);
    expect(result.current.items[0]?.acknowledgedAt).toBe('2026-08-04T12:05:00.000Z');
  });

  it('convite de push só fica pendente DEPOIS do primeiro alerta da sessão (US-081 §10 — nunca no primeiro acesso)', async () => {
    vi.stubGlobal('Notification', FakeNotification);
    const api = {
      listUnread: vi.fn().mockResolvedValue({ alerts: [], nextCursor: null }),
    } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));
    await waitFor(() => expect(result.current.loading).toBe(false));

    // Ainda sem nenhum alerta visto nesta sessão — nunca pede permissão de saída.
    expect(result.current.pushPermissionPending).toBe(false);

    act(() => {
      emitAlert({ type: 'alert.raised', data: { alertId: 'x' } });
    });

    await waitFor(() => expect(result.current.pushPermissionPending).toBe(true));
  });

  it('requestPushPermission: pede permissão e assina push quando concedida', async () => {
    FakeNotification.permission = 'default';
    vi.stubGlobal('Notification', FakeNotification);
    FakeNotification.requestPermission = vi.fn(async (): Promise<NotificationPermission> => 'granted');
    const api = {
      listUnread: vi.fn().mockResolvedValue({ alerts: [alertFixture()], nextCursor: null }),
    } as unknown as NotificationCenterApi;

    const { result } = renderHook(() => useNotificationCenter({ identity, api }));
    await waitFor(() => expect(result.current.pushPermissionPending).toBe(true));

    await act(async () => {
      await result.current.requestPushPermission();
    });

    expect(FakeNotification.requestPermission).toHaveBeenCalledOnce();
    expect(subscribeToPush).toHaveBeenCalledWith({ identity });
    await waitFor(() => expect(result.current.pushPermissionPending).toBe(false));
  });
});
