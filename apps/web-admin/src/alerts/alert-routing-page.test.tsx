// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});
import { alertEngineTypes, type AlertRoutingConfig, type RoleDto } from '@nexora/contracts';
import type { AlertRoutingApi } from './alert-routing-api.js';
import { AlertRoutingPage } from './alert-routing-page.js';

const roles: readonly RoleDto[] = [
  {
    id: '0198aabb-3333-7000-8000-000000000001',
    code: 'WAITER',
    name: 'Garçom',
    permissions: [],
    system: true,
    userCount: 4,
  },
  {
    id: '0198aabb-3333-7000-8000-000000000002',
    code: 'MANAGER',
    name: 'Gestor',
    permissions: [],
    system: true,
    userCount: 1,
  },
];

function baseConfig(): AlertRoutingConfig {
  return Object.fromEntries(
    alertEngineTypes.map((type) => [
      type,
      { roles: [], scope: 'TENANT', escalateAfterSeconds: null, groupWindowSeconds: null },
    ]),
  );
}

describe('AlertRoutingPage', () => {
  it('carrega e exibe o direcionamento atual do primeiro tipo de alerta (US-082 §10)', async () => {
    const config: AlertRoutingConfig = {
      ...baseConfig(),
      ORDER_LATE: {
        roles: ['WAITER'],
        scope: 'RESPONSIBLE',
        escalateAfterSeconds: 120,
        groupWindowSeconds: null,
      },
    };
    const get = vi.fn().mockResolvedValue(config);
    const alertRoutingApi = { get, update: vi.fn() } as unknown as AlertRoutingApi;

    render(<AlertRoutingPage roles={roles} alertRoutingApi={alertRoutingApi} />);

    expect(await screen.findByLabelText('Garçom')).toBeChecked();
    expect(screen.getAllByText('Pedido atrasado').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByLabelText('Gestor')).not.toBeChecked();
    expect(screen.getByLabelText('Escopo')).toHaveValue('RESPONSIBLE');
    expect(screen.getByLabelText('Escalonar após (segundos)')).toHaveValue(120);
    expect(screen.getByText(/Vai para: Garçom/)).toBeInTheDocument();
  });

  it('adicionar um papel e salvar dispara PATCH só com a regra do tipo selecionado', async () => {
    const config: AlertRoutingConfig = {
      ...baseConfig(),
      ORDER_LATE: {
        roles: ['WAITER'],
        scope: 'RESPONSIBLE',
        escalateAfterSeconds: 120,
        groupWindowSeconds: null,
      },
    };
    const get = vi.fn().mockResolvedValue(config);
    const update = vi.fn().mockResolvedValue({
      ...config,
      ORDER_LATE: { ...config.ORDER_LATE, roles: ['WAITER', 'MANAGER'] },
    });
    const alertRoutingApi = { get, update } as unknown as AlertRoutingApi;

    render(<AlertRoutingPage roles={roles} alertRoutingApi={alertRoutingApi} />);
    await screen.findByLabelText('Garçom');

    fireEvent.click(screen.getByLabelText('Gestor'));
    fireEvent.click(screen.getByRole('button', { name: 'Salvar direcionamento' }));

    await waitFor(() => expect(update).toHaveBeenCalledTimes(1));
    expect(update).toHaveBeenCalledWith({
      ORDER_LATE: { roles: ['WAITER', 'MANAGER'] },
    });
  });

  it('erro de carregamento mostra estado de erro', async () => {
    const get = vi.fn().mockRejectedValue(new Error('Falha ao consultar'));
    const alertRoutingApi = { get, update: vi.fn() } as unknown as AlertRoutingApi;

    render(<AlertRoutingPage roles={roles} alertRoutingApi={alertRoutingApi} />);

    expect(await screen.findByText('Falha ao consultar')).toBeInTheDocument();
    expect(screen.queryByLabelText('Escopo')).not.toBeInTheDocument();
  });
});
