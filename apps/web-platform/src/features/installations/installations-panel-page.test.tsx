// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type {
  InstallationDiagnosticsResponse,
  InstallationIncident,
  PlatformInstallationSummary,
} from '@nexora/contracts';
import type { InstallationsApi } from './installations-api.js';
import { InstallationsPanelPage } from './installations-panel-page.js';

const okInstallation: PlatformInstallationSummary = {
  installationId: '0198aabb-0001-7000-8000-000000000001',
  tenantId: '0198aabb-0001-7000-8000-000000000002',
  tenantName: 'Pizzaria Dona Betinha',
  storeName: 'Matriz',
  version: '1.4.2',
  expectedVersion: '1.4.2',
  lastSeenAt: new Date().toISOString(),
  syncLagSeconds: 4,
  pendingEvents: 0,
  openAlerts: 0,
  health: 'OK',
};

const downInstallation: PlatformInstallationSummary = {
  installationId: '0198aabb-0002-7000-8000-000000000001',
  tenantId: '0198aabb-0002-7000-8000-000000000002',
  tenantName: 'Pizzaria do Zé',
  storeName: 'Centro',
  version: '1.3.0',
  expectedVersion: '1.4.2',
  lastSeenAt: new Date(Date.now() - 30 * 60_000).toISOString(),
  syncLagSeconds: 1800,
  pendingEvents: 42,
  openAlerts: 1,
  health: 'DOWN',
};

const degradedInstallation: PlatformInstallationSummary = {
  ...downInstallation,
  installationId: '0198aabb-0003-7000-8000-000000000001',
  tenantName: 'Pizzaria Degradada',
  health: 'DEGRADED',
};

function buildApi(overrides: Partial<InstallationsApi> = {}): InstallationsApi {
  return {
    list: vi.fn().mockResolvedValue([okInstallation, downInstallation]),
    diagnostics: vi.fn().mockResolvedValue({
      healthCheck: { postgres: 'OK', redis: 'OK', sync: 'OK', checkedAt: new Date().toISOString() },
      recentLogs: [{ occurredAt: new Date().toISOString(), level: 'ERROR', message: 'Sem contato com a nuvem.' }],
      diskUsagePercent: 42,
      lastBackupAt: null,
    } satisfies InstallationDiagnosticsResponse),
    incidents: vi.fn().mockResolvedValue([
      {
        id: 'incident-1',
        type: 'OFFLINE',
        startedAt: new Date(Date.now() - 30 * 60_000).toISOString(),
        resolvedAt: null,
        cause: 'Sem contato com a nuvem há 30 minutos.',
        durationSeconds: 1800,
      } satisfies InstallationIncident,
    ]),
    ...overrides,
  };
}

describe('InstallationsPanelPage', () => {
  it('carrega e lista as instalações com saúde, versão e atraso de sincronização (US-140 §4)', async () => {
    const api = buildApi();
    render(<InstallationsPanelPage api={api} />);

    expect(await screen.findByText('Pizzaria Dona Betinha')).toBeInTheDocument();
    expect(screen.getByText('Pizzaria do Zé')).toBeInTheDocument();
    expect(screen.getByText('Saudável')).toBeInTheDocument();
    expect(screen.getByText('Fora do ar')).toBeInTheDocument();
    expect(api.list).toHaveBeenCalledTimes(1);
  });

  it('distingue DEGRADED de DOWN por cor e rótulo (US-140 §4, cenário "Instalação degradada")', async () => {
    const api = buildApi({ list: vi.fn().mockResolvedValue([downInstallation, degradedInstallation]) });
    render(<InstallationsPanelPage api={api} />);

    const downBadge = await screen.findByText('Fora do ar');
    const degradedBadge = await screen.findByText('Degradada');
    expect(downBadge).toBeInTheDocument();
    expect(degradedBadge).toBeInTheDocument();
    expect(downBadge.className).not.toBe(degradedBadge.className);
  });

  it('instalação desatualizada mostra a deriva de versão (US-140 §4, cenário "Deriva de versão")', async () => {
    const api = buildApi();
    render(<InstallationsPanelPage api={api} />);

    await screen.findByText('Pizzaria do Zé');
    expect(screen.getByText('1.3.0')).toBeInTheDocument();
    expect(screen.getByText('desatualizada')).toBeInTheDocument();
    // A instalação em dia (mesma versão esperada) não ganha o badge de deriva.
    expect(screen.queryAllByText('desatualizada')).toHaveLength(1);
  });

  it('abrir uma linha mostra o diagnóstico e o histórico de incidentes sem sair da tela (US-140 §4/§10)', async () => {
    const api = buildApi();
    render(<InstallationsPanelPage api={api} />);

    fireEvent.click(await screen.findByText('Pizzaria do Zé'));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    const dialog = screen.getByRole('dialog');
    expect(await within(dialog).findByText('Sem contato com a nuvem.')).toBeInTheDocument();
    expect(within(dialog).getByText('Sem contato com a nuvem há 30 minutos.')).toBeInTheDocument();
    expect(api.diagnostics).toHaveBeenCalledWith(downInstallation.installationId);
    expect(api.incidents).toHaveBeenCalledWith(downInstallation.installationId);
  });

  it('erro ao carregar a lista mostra estado de erro', async () => {
    const api = buildApi({ list: vi.fn().mockRejectedValue(new Error('Falha de rede')) });
    render(<InstallationsPanelPage api={api} />);

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
  });

  it('sem instalações ativas mostra estado vazio', async () => {
    const api = buildApi({ list: vi.fn().mockResolvedValue([]) });
    render(<InstallationsPanelPage api={api} />);

    expect(await screen.findByText('Nenhuma instalação ativa')).toBeInTheDocument();
  });
});
