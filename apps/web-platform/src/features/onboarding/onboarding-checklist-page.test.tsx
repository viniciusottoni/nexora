// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { OnboardingStatusResponse } from '@nexora/contracts';
import type { OnboardingApi, OnboardingApiProblem } from './onboarding-api.js';
import { OnboardingChecklistPage } from './onboarding-checklist-page.js';

afterEach(() => {
  cleanup();
});

function buildStatus(overrides: Partial<OnboardingStatusResponse['steps'][number]>[] = []): OnboardingStatusResponse {
  const base: OnboardingStatusResponse['steps'] = [
    { key: 'TENANT_CREATED', status: 'DONE' },
    { key: 'BRANDING', status: 'DONE' },
    { key: 'MENU', status: 'IN_PROGRESS', progress: { products: 44, expected: 60 } },
    { key: 'TABLES', status: 'PENDING' },
    { key: 'EDGE_INSTALL', status: 'PENDING' },
    { key: 'PAYMENT_CONFIG', status: 'PENDING' },
    { key: 'TRAINING', status: 'PENDING' },
    { key: 'PILOT', status: 'PENDING' },
    { key: 'ACTIVATION', status: 'PENDING' },
  ];
  return {
    steps: base.map((step, index) => ({ ...step, ...overrides[index] })),
    startedAt: '2026-08-01T09:00:00.000Z',
    elapsedBusinessDays: 2,
  };
}

describe('OnboardingChecklistPage', () => {
  it('mostra os nove passos com progresso quantificado (44 de 60 produtos)', async () => {
    const api = { getStatus: vi.fn(async () => buildStatus()) } as unknown as OnboardingApi;

    render(<OnboardingChecklistPage tenantId="tenant-1" api={api} />);

    expect(await screen.findByText('44 de 60 produtos')).toBeInTheDocument();
    expect(screen.getByText('2/9')).toBeInTheDocument();
    expect(screen.getByText(/2 dias úteis decorridos/)).toBeInTheDocument();
  });

  it('ativação bloqueada exibe os passos pendentes retornados pela API', async () => {
    const getStatus = vi.fn(async () => buildStatus());
    const activate = vi.fn(async () => {
      const error = new Error('Ainda há passos pendentes no roteiro de implantação.') as OnboardingApiProblem;
      error.code = 'ONBOARDING_INCOMPLETE';
      error.pendingSteps = ['MENU', 'TABLES', 'TRAINING'];
      throw error;
    });
    const api = { getStatus, activate } as unknown as OnboardingApi;

    render(<OnboardingChecklistPage tenantId="tenant-1" api={api} />);

    await screen.findByText('44 de 60 produtos');
    fireEvent.click(screen.getByRole('button', { name: 'Ativar estabelecimento' }));

    await waitFor(() => expect(activate).toHaveBeenCalledWith('tenant-1'));
    const bannerTitle = await screen.findByText('Ativação bloqueada');
    const banner = bannerTitle.closest('[role="status"]') ?? bannerTitle.parentElement!;
    expect(within(banner as HTMLElement).getByText('Cardápio')).toBeInTheDocument();
    expect(within(banner as HTMLElement).getByText('Mesas')).toBeInTheDocument();
    expect(within(banner as HTMLElement).getByText('Treinamento da equipe')).toBeInTheDocument();
  });

  it('ativação bem sucedida recarrega o status', async () => {
    const getStatus = vi
      .fn()
      .mockResolvedValueOnce(buildStatus())
      .mockResolvedValueOnce(
        buildStatus([{}, {}, { status: 'DONE' }, { status: 'DONE' }, { status: 'DONE' }, { status: 'DONE' }, { status: 'DONE' }, { status: 'DONE' }, { status: 'DONE' }]),
      );
    const activate = vi.fn(async () => undefined);
    const api = { getStatus, activate } as unknown as OnboardingApi;

    render(<OnboardingChecklistPage tenantId="tenant-1" api={api} />);

    await screen.findByText('44 de 60 produtos');
    fireEvent.click(screen.getByRole('button', { name: 'Ativar estabelecimento' }));

    await waitFor(() => expect(getStatus).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('9/9')).toBeInTheDocument();
  });
});
