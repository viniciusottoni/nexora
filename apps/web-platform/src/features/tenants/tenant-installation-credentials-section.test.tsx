// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { TenantInstallationCredentialsSection } from './tenant-installation-credentials-section.js';
import type {
  InstallationCredentialsApi,
  ReissueInstallationTokenResult,
  TenantDeploymentStatus,
} from './installation-credentials-api.js';

afterEach(() => {
  cleanup();
});

const pendingStatus: TenantDeploymentStatus = {
  completed: 1,
  total: 9,
  installation: { id: 'installation-1', status: 'PENDING', canReissueToken: true },
  nextAction: 'BRANDING',
};

const installedStatus: TenantDeploymentStatus = {
  completed: 5,
  total: 9,
  installation: { id: 'installation-1', status: 'ACTIVE', canReissueToken: false },
  nextAction: 'PAYMENT_CONFIG',
};

const reissueResult: ReissueInstallationTokenResult = {
  credentialId: 'credential-1',
  expiresAt: new Date('2026-08-07T12:00:00Z').toISOString(),
  installToken: 'raw-token-abcdefgh12345678',
  installCommand: './install.sh --tenant=tenant-1 --token=raw-token-abcdefgh12345678',
};

function buildApi(overrides?: Partial<InstallationCredentialsApi>): InstallationCredentialsApi {
  return {
    getDeploymentStatus: vi.fn(async () => pendingStatus),
    reissueToken: vi.fn(async () => reissueResult),
    revokeCredential: vi.fn(async () => undefined),
    ...overrides,
  };
}

describe('TenantInstallationCredentialsSection', () => {
  it('mostra o checklist reconstruído e diferencia instalação registrada de token disponível', async () => {
    const api = buildApi();
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    await waitFor(() => expect(api.getDeploymentStatus).toHaveBeenCalledWith('tenant-1'));

    expect(await screen.findByText('1/9')).toBeInTheDocument();
    expect(screen.getByText('Ainda não registrada')).toBeInTheDocument();
    expect(screen.getByText('Disponível para reemissão')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Reemitir token de instalação/ }),
    ).toBeInTheDocument();
  });

  it('nao oferece reemissao quando a instalacao ja foi pareada', async () => {
    const api = buildApi({ getDeploymentStatus: vi.fn(async () => installedStatus) });
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    await screen.findByText('5/9');

    expect(screen.getByText('Ativa')).toBeInTheDocument();
    expect(screen.getByText('Não se aplica (já pareada)')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Reemitir token de instalação/ }),
    ).not.toBeInTheDocument();
  });

  it('reemite o token exigindo motivo e mostra o segredo mascarado por padrão', async () => {
    const api = buildApi();
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    await screen.findByRole('button', { name: /Reemitir token de instalação/ });
    fireEvent.click(screen.getByRole('button', { name: /Reemitir token de instalação/ }));

    // Motivo vazio não deixa confirmar.
    const confirmButton = screen.getByRole('button', { name: 'Sim, reemitir token' });
    expect(confirmButton).toBeDisabled();

    fireEvent.change(screen.getByLabelText('Motivo'), {
      target: { value: 'Comando original não foi exibido' },
    });
    expect(confirmButton).toBeEnabled();

    fireEvent.click(confirmButton);

    await waitFor(() =>
      expect(api.reissueToken).toHaveBeenCalledWith('installation-1', {
        reason: 'Comando original não foi exibido',
        expiresInHours: 24,
      }),
    );

    await screen.findByText('Token gerado');
    expect(screen.queryByText(reissueResult.installToken!)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Revelar token' }));
    expect(screen.getByText(reissueResult.installToken!)).toBeInTheDocument();
  });

  it('identifica motivo e validade da reemissao como campos administrativos', async () => {
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={buildApi()} />);

    fireEvent.click(await screen.findByRole('button', { name: /Reemitir token/ }));

    expect(screen.getByLabelText('Motivo')).toHaveAttribute(
      'name',
      'installation-token-reissue-reason',
    );
    expect(screen.getByLabelText('Motivo')).toHaveAttribute('autocomplete', 'off');
    expect(screen.getByLabelText('Validade')).toHaveAttribute(
      'name',
      'installation-token-expiration',
    );
  });

  it('copia o token para a área de transferência e mostra confirmação', async () => {
    const writeText = vi.fn(async () => undefined);
    Object.assign(navigator, { clipboard: { writeText } });

    const api = buildApi();
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    fireEvent.click(await screen.findByRole('button', { name: /Reemitir token de instalação/ }));
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Comando perdido' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sim, reemitir token' }));

    await screen.findByText('Token gerado');
    fireEvent.click(screen.getByRole('button', { name: /Copiar token/ }));

    await waitFor(() => expect(writeText).toHaveBeenCalledWith(reissueResult.installToken));
    expect(
      await screen.findByText(
        'Token copiado. Guarde-o em local seguro — ele não será mostrado novamente.',
      ),
    ).toBeInTheDocument();
  });

  it('trata a repetição idempotente (installToken nulo) sem tentar exibir um segredo inexistente', async () => {
    const api = buildApi({
      reissueToken: vi.fn(async () => ({
        ...reissueResult,
        installToken: null,
        installCommand: null,
      })),
    });
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    fireEvent.click(await screen.findByRole('button', { name: /Reemitir token de instalação/ }));
    fireEvent.change(screen.getByLabelText('Motivo'), {
      target: { value: 'Repetindo a mesma intenção' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Sim, reemitir token' }));

    await screen.findByText('Segredo já exibido');
    expect(screen.queryByRole('button', { name: 'Revelar token' })).not.toBeInTheDocument();
  });

  it('revoga a credencial recém-emitida exigindo motivo', async () => {
    const api = buildApi();
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    fireEvent.click(await screen.findByRole('button', { name: /Reemitir token de instalação/ }));
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Comando perdido' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sim, reemitir token' }));

    await screen.findByText('Token gerado');
    fireEvent.click(screen.getByRole('button', { name: /Revogar agora/ }));

    const revokeConfirm = screen.getByRole('button', { name: 'Sim, revogar credencial' });
    expect(revokeConfirm).toBeDisabled();

    fireEvent.change(screen.getByLabelText('Motivo', { selector: 'input' }), {
      target: { value: 'Credencial possivelmente exposta' },
    });

    fireEvent.click(screen.getByRole('button', { name: 'Sim, revogar credencial' }));

    await waitFor(() =>
      expect(api.revokeCredential).toHaveBeenCalledWith(
        'installation-1',
        'credential-1',
        'Credencial possivelmente exposta',
      ),
    );
    expect(await screen.findByText('Credencial revogada')).toBeInTheDocument();
  });

  it('nunca grava o segredo em localStorage ou sessionStorage', async () => {
    const api = buildApi();
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    fireEvent.click(await screen.findByRole('button', { name: /Reemitir token de instalação/ }));
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Comando perdido' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sim, reemitir token' }));

    await screen.findByText('Token gerado');

    const allStorageValues = [
      ...Array.from({ length: localStorage.length }, (_, i) =>
        localStorage.getItem(localStorage.key(i)!),
      ),
      ...Array.from({ length: sessionStorage.length }, (_, i) =>
        sessionStorage.getItem(sessionStorage.key(i)!),
      ),
    ];

    expect(allStorageValues.some((value) => value?.includes(reissueResult.installToken!))).toBe(
      false,
    );
  });

  it('mostra erro quando o checklist falha ao carregar', async () => {
    const api = buildApi({
      getDeploymentStatus: vi.fn(async () => {
        throw new Error('Estabelecimento não encontrado.');
      }),
    });
    render(<TenantInstallationCredentialsSection tenantId="tenant-1" api={api} />);

    await screen.findByText('Estabelecimento não encontrado.');
  });
});
