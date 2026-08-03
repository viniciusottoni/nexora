// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DeviceManagementPage } from './device-management-page.js';

const device = {
  id: '0198aabb-1111-7000-8000-000000000001',
  label: 'Celular garçom',
  kind: 'WAITER' as const,
  active: true,
  lastSeenAt: '2026-06-01T18:00:00.000Z',
  needsReview: true,
};

const revokedDevice = { ...device, id: '0198aabb-2222-7000-8000-000000000002', active: false };

describe('DeviceManagementPage', () => {
  it('orienta pareamento local sem chamar rota de criaÃ§Ã£o na nuvem', () => {
    render(
      <DeviceManagementPage
        devices={[device]}
        onRename={async () => undefined}
        onRevoke={async () => undefined}
        onDelete={async () => undefined}
      />,
    );

    expect(
      screen.queryByRole('button', { name: 'Autorizar novo dispositivo' }),
    ).not.toBeInTheDocument();
    expect(screen.getByText(/painel local da loja/i)).toBeInTheDocument();
  });

  it('mostra código grande, papel, último acesso e revisão por inatividade', async () => {
    render(
      <DeviceManagementPage
        devices={[device]}
        onCreatePairingCode={async () => ({
          code: '418302',
          expiresAt: '2026-07-31T18:10:00.000Z',
        })}
        onRename={async () => undefined}
        onRevoke={async () => undefined}
        onDelete={async () => undefined}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Autorizar novo dispositivo' }));

    expect(await screen.findByRole('status')).toHaveTextContent('418302');
    expect(screen.getByText('Celular de garçom')).toBeInTheDocument();
    expect(screen.getByText('Sem acesso há mais de 30 dias')).toBeInTheDocument();
    expect(screen.getByText('Último acesso:')).toBeInTheDocument();
  });

  it('exige confirmação explícita e avisa sobre encerramento de sessões', async () => {
    const onRevoke = vi.fn(async () => undefined);
    render(
      <DeviceManagementPage
        devices={[device]}
        onCreatePairingCode={async () => ({
          code: '418302',
          expiresAt: '2026-07-31T18:10:00.000Z',
        })}
        onRename={async () => undefined}
        onRevoke={onRevoke}
        onDelete={async () => undefined}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Revogar Celular garçom' }));
    const dialog = screen.getByRole('dialog', { name: 'Revogar dispositivo?' });
    expect(dialog).toHaveTextContent('Todas as sessões ativas serão encerradas imediatamente');
    fireEvent.click(screen.getByRole('button', { name: 'Sim, revogar dispositivo' }));

    await waitFor(() => expect(onRevoke).toHaveBeenCalledWith(device.id));
  });

  it('só oferece excluir para dispositivo já revogado, e exige confirmação', async () => {
    const onDelete = vi.fn(async () => undefined);
    render(
      <DeviceManagementPage
        devices={[revokedDevice]}
        onRename={async () => undefined}
        onRevoke={async () => undefined}
        onDelete={onDelete}
      />,
    );

    expect(
      screen.queryByRole('button', { name: `Revogar ${revokedDevice.label}` }),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: `Excluir ${revokedDevice.label}` }));
    const dialog = screen.getByRole('dialog', { name: 'Excluir dispositivo?' });
    expect(dialog).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Sim, excluir dispositivo' }));

    await waitFor(() => expect(onDelete).toHaveBeenCalledWith(revokedDevice.id));
  });
});
