// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, waitFor, fireEvent, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { OnboardingStatusResponse } from '@nexora/contracts';
import type { OnboardingApi } from './onboarding-api.js';
import { OnboardingSetupPage } from './onboarding-setup-page.js';

function buildStatus(overrides: Partial<OnboardingStatusResponse['steps'][number]>[] = []): OnboardingStatusResponse {
  const base: OnboardingStatusResponse['steps'] = [
    { key: 'TENANT_CREATED', status: 'DONE' },
    { key: 'BRANDING', status: 'DONE' },
    { key: 'MENU', status: 'IN_PROGRESS', progress: { products: 12, expected: null } },
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

describe('OnboardingSetupPage', () => {
  it('carrega e exibe o roteiro em linguagem de negócio', async () => {
    const getStatus = vi.fn(async () => buildStatus());
    const api = { getStatus } as unknown as OnboardingApi;

    const { container } = render(<OnboardingSetupPage tenantId="tenant-1" api={api} />);
    const page = within(container);

    expect(await page.findByText('Identidade visual')).toBeInTheDocument();
    expect(page.getByText('Cardápio', { selector: 'strong' })).toBeInTheDocument();
    expect(page.getByText('2/9')).toBeInTheDocument();
    expect(getStatus).toHaveBeenCalledWith('tenant-1');
  });

  it('mostra a contagem real de produtos no passo de cardápio', async () => {
    const api = { getStatus: vi.fn(async () => buildStatus()) } as unknown as OnboardingApi;

    const { container } = render(<OnboardingSetupPage tenantId="tenant-1" api={api} />);
    const page = within(container);

    expect(await page.findByText(/12 produtos cadastrados/)).toBeInTheDocument();
  });

  it('permite marcar TRAINING como concluído manualmente', async () => {
    const completeStep = vi.fn(async () => undefined);
    const getStatus = vi
      .fn()
      .mockResolvedValueOnce(buildStatus())
      .mockResolvedValueOnce(buildStatus([{}, {}, {}, {}, {}, {}, { status: 'DONE' }]));
    const api = { getStatus, completeStep } as unknown as OnboardingApi;

    const { container } = render(<OnboardingSetupPage tenantId="tenant-1" api={api} />);
    const page = within(container);

    await page.findAllByText('Treinamento da equipe');
    fireEvent.click(page.getAllByRole('button', { name: 'Marcar concluído' })[0]!);

    await waitFor(() => expect(completeStep).toHaveBeenCalledWith('tenant-1', 'TRAINING'));
    await waitFor(() => expect(getStatus).toHaveBeenCalledTimes(2));
  });

  it('nao oferece marcar concluido para passos derivados como MENU', async () => {
    const api = { getStatus: vi.fn(async () => buildStatus()) } as unknown as OnboardingApi;

    const { container } = render(<OnboardingSetupPage tenantId="tenant-1" api={api} />);
    const page = within(container);

    await page.findByText('Cardápio', { selector: 'strong' });
    // MENU está IN_PROGRESS mas não é autoatendível manualmente (US-141: sem meta confiável) —
    // só mostra o badge de status, não um botão de ação.
    expect(page.getAllByRole('button', { name: 'Marcar concluído' })).toHaveLength(2); // TRAINING + PILOT
  });

  it('exibe erro quando o carregamento falha', async () => {
    const api = {
      getStatus: vi.fn(async () => {
        throw new Error('Não foi possível carregar a implantação.');
      }),
    } as unknown as OnboardingApi;

    const { container } = render(<OnboardingSetupPage tenantId="tenant-1" api={api} />);
    const page = within(container);

    expect(await page.findByText('Não foi possível carregar a implantação.')).toBeInTheDocument();
  });
});
