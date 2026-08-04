// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
});
import type { Alert } from '@nexora/contracts';
import type { NotificationsApi } from './notifications-api.js';
import { NotificationBell } from './notification-bell.js';

const pendingAlert: Alert = {
  id: '0198aabb-4444-7000-8000-000000000001',
  type: 'ORDER_LATE',
  severity: 'HIGH',
  entityType: 'order',
  entityId: '0198aabb-4444-7000-8000-000000000002',
  message: 'Pedido A47 da mesa 12 está há 21 minutos na fila.',
  raisedAt: new Date().toISOString(),
  acknowledgedAt: null,
  acknowledgedBy: null,
  resolvedAt: null,
  targetRoles: ['MANAGER'],
  targetUserId: null,
  groupKey: null,
};

describe('NotificationBell', () => {
  it('busca pendentes na montagem e mostra o contador no sino (US-081 §10)', async () => {
    const listUnread = vi.fn().mockResolvedValue([pendingAlert]);
    const notificationsApi = { listUnread, acknowledge: vi.fn() } as unknown as NotificationsApi;

    render(<NotificationBell notificationsApi={notificationsApi} />);

    await waitFor(() => expect(listUnread).toHaveBeenCalledTimes(1));
    expect(
      await screen.findByRole('button', { name: /Notificações — 1 pendentes/ }),
    ).toBeInTheDocument();
  });

  it('reconhecer um alerta chama a API e atualiza a lista', async () => {
    const listUnread = vi.fn().mockResolvedValue([pendingAlert]);
    const acknowledge = vi.fn().mockResolvedValue(undefined);
    const notificationsApi = { listUnread, acknowledge } as unknown as NotificationsApi;

    render(<NotificationBell notificationsApi={notificationsApi} />);
    const trigger = await screen.findByRole('button', { name: /Notificações/ });
    fireEvent.click(trigger);

    const acknowledgeButton = await screen.findByRole('button', { name: 'Reconhecer' });
    fireEvent.click(acknowledgeButton);

    await waitFor(() => expect(acknowledge).toHaveBeenCalledWith(pendingAlert.id));
  });
});
