// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type {
  ApiProblem,
  PlatformPlanSummary,
  TenantPlanApi,
  TenantPlanReconcileResult,
  TenantPlanUpdateResult,
  TenantPlanView,
} from './tenant-plan-api.js';
import { TenantPlanSection } from './tenant-plan-section.js';

const TENANT_ID = '0198aabb-0001-7000-8000-000000000001';

const catalog: PlatformPlanSummary[] = [
  { code: 'STANDARD', name: 'Standard', active: true, capabilities: ['online_ordering', 'kds'] },
  {
    code: 'GESTAO',
    name: 'Gestão',
    active: true,
    capabilities: ['online_ordering', 'kds', 'inventory'],
  },
  {
    code: 'COMPLETO',
    name: 'Completo',
    active: true,
    capabilities: ['online_ordering', 'kds', 'inventory', 'multi_store'],
  },
];

const consistentPlan: TenantPlanView = {
  current: 'GESTAO',
  effectiveCapabilities: ['online_ordering', 'kds', 'inventory'],
  scheduled: null,
  consistent: true,
  version: 3,
  history: [],
};

const divergentPlan: TenantPlanView = {
  ...consistentPlan,
  effectiveCapabilities: [],
  consistent: false,
};

const scheduledPlan: TenantPlanView = {
  ...consistentPlan,
  scheduled: { plan: 'COMPLETO', effectiveAt: '2026-09-01T00:00:00Z' },
};

const planWithHistory = {
  ...consistentPlan,
  history: [
    {
      previous: 'STANDARD',
      next: 'GESTAO',
      requestedAt: '2026-08-01T10:00:00Z',
      effectiveAt: '2026-08-01T10:00:00Z',
      appliedAt: '2026-08-01T10:00:01Z',
      reason: 'Ampliação dos módulos contratados',
      actorId: '0198aabb-1111-7000-8000-000000000001',
    },
  ],
};

const updateResult: TenantPlanUpdateResult = {
  current: 'GESTAO',
  scheduled: { plan: 'COMPLETO', effectiveAt: '2026-09-01T00:00:00Z' },
  version: 4,
};

const reconcileResult: TenantPlanReconcileResult = {
  current: 'GESTAO',
  effectiveCapabilities: ['online_ordering', 'kds', 'inventory'],
  consistent: true,
  changed: true,
};

function buildApi(overrides: Partial<TenantPlanApi> = {}): TenantPlanApi {
  return {
    listPlans: vi.fn().mockResolvedValue(catalog),
    get: vi.fn().mockResolvedValue(consistentPlan),
    update: vi.fn().mockResolvedValue(updateResult),
    reconcile: vi.fn().mockResolvedValue(reconcileResult),
    ...overrides,
  };
}

function genericError(): ApiProblem {
  return new Error('Falha de rede');
}

describe('TenantPlanSection', () => {
  it('mostra o plano corrente, versão, capacidades efetivas e badge de consistência vindos do servidor (nunca um valor otimista)', async () => {
    const api = buildApi();
    render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('GESTAO')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('Nenhuma mudança agendada')).toBeInTheDocument();
    expect(screen.getByText('Consistente')).toBeInTheDocument();
    expect(screen.getByText('online_ordering')).toBeInTheDocument();
    expect(screen.getByText('inventory')).toBeInTheDocument();
    expect(api.get).toHaveBeenCalledWith(TENANT_ID);
    expect(api.listPlans).toHaveBeenCalledTimes(1);
  });

  it('agendamento pendente aparece no resumo em vez de "Nenhuma mudança agendada"', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(scheduledPlan) });
    render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText(/COMPLETO a partir de/)).toBeInTheDocument();
  });

  it('exibe o histórico temporal com antes/depois, vigência, motivo, autor e situação', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(planWithHistory) });
    render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByRole('heading', { name: 'Histórico de planos' })).toBeInTheDocument();
    const historyTable = screen.getByRole('table', { name: 'Histórico de planos' });
    expect(
      within(historyTable).getByRole('row', { name: /STANDARD para GESTAO/ }),
    ).toBeInTheDocument();
    expect(within(historyTable).getByText('Ampliação dos módulos contratados')).toBeInTheDocument();
    expect(within(historyTable).getByText('Efetivado')).toBeInTheDocument();
    expect(
      within(historyTable).getByText('0198aabb-1111-7000-8000-000000000001'),
    ).toBeInTheDocument();
  });

  it('divergência mostra AlertBanner com ação de reconciliar (US-154 §4, "sem correção automática silenciosa")', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(divergentPlan) });
    render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Divergência entre plano e configuração')).toBeInTheDocument();
    expect(screen.getByText('Divergente')).toBeInTheDocument();
    expect(api.reconcile).not.toHaveBeenCalled();
  });

  it('clicar em "Reconciliar agora" chama a API e recarrega o plano', async () => {
    const get = vi.fn().mockResolvedValueOnce(divergentPlan).mockResolvedValueOnce(consistentPlan);
    const reconcile = vi.fn().mockResolvedValue(reconcileResult);
    const api = buildApi({ get, reconcile });
    render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

    const button = await screen.findByRole('button', { name: 'Reconciliar agora' });
    fireEvent.click(button);

    await waitFor(() => expect(reconcile).toHaveBeenCalledWith(TENANT_ID));
    await waitFor(() => expect(get).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('Consistente')).toBeInTheDocument();
  });

  it('erro ao carregar mostra AlertBanner de perigo', async () => {
    const api = buildApi({ get: vi.fn().mockRejectedValue(genericError()) });
    render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
  });

  describe('Alterar plano', () => {
    it('exige plano, data de vigência e motivo antes de habilitar a confirmação (US-154 §10)', async () => {
      const api = buildApi();
      render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Alterar plano/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Alterar plano' });
      const confirmButton = within(dialog).getByRole('button', { name: 'Confirmar mudança' });

      expect(confirmButton).toBeDisabled();

      fireEvent.change(within(dialog).getByLabelText(/^Novo plano/), {
        target: { value: 'COMPLETO' },
      });
      expect(confirmButton).toBeDisabled();

      fireEvent.change(within(dialog).getByLabelText(/^Data de vigência/), {
        target: { value: '2026-09-01T00:00' },
      });
      expect(confirmButton).toBeDisabled();

      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), {
        target: { value: 'Aditivo contratual #32' },
      });
      expect(confirmButton).toBeEnabled();
    });

    it('mostra a comparação antes/depois com capacidades ganhas e perdidas', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(consistentPlan) });
      render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Alterar plano/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Alterar plano' });

      fireEvent.change(within(dialog).getByLabelText(/^Novo plano/), {
        target: { value: 'STANDARD' },
      });

      expect(within(dialog).getByText('Antes (GESTAO)')).toBeInTheDocument();
      expect(within(dialog).getByText('Depois (Standard)')).toBeInTheDocument();
      // GESTAO tem "inventory" e STANDARD não — é uma perda (downgrade).
      expect(within(dialog).getByText('Este é um downgrade')).toBeInTheDocument();
    });

    it('confirma a mudança com Idempotency-Key/If-Match implícitos no client e recarrega o plano depois', async () => {
      const get = vi
        .fn()
        .mockResolvedValueOnce(consistentPlan)
        .mockResolvedValueOnce(scheduledPlan);
      const update = vi.fn().mockResolvedValue(updateResult);
      const api = buildApi({ get, update });
      render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Alterar plano/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Alterar plano' });

      fireEvent.change(within(dialog).getByLabelText(/^Novo plano/), {
        target: { value: 'COMPLETO' },
      });
      fireEvent.change(within(dialog).getByLabelText(/^Data de vigência/), {
        target: { value: '2026-09-01T00:00' },
      });
      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), {
        target: { value: 'Aditivo contratual #32' },
      });

      fireEvent.click(within(dialog).getByRole('button', { name: 'Confirmar mudança' }));

      await waitFor(() =>
        expect(update).toHaveBeenCalledWith(
          TENANT_ID,
          3,
          expect.objectContaining({ plan: 'COMPLETO', reason: 'Aditivo contratual #32' }),
        ),
      );
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(get).toHaveBeenCalledTimes(2);
    });

    it('erro do servidor (ex.: PLAN_NOT_AVAILABLE) aparece no modal e mantém o diálogo aberto', async () => {
      const error = new Error('Plano comercial não disponível.') as ApiProblem;
      error.code = 'PLAN_NOT_AVAILABLE';
      error.status = 422;
      const api = buildApi({ update: vi.fn().mockRejectedValue(error) });
      render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Alterar plano/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Alterar plano' });

      fireEvent.change(within(dialog).getByLabelText(/^Novo plano/), {
        target: { value: 'COMPLETO' },
      });
      fireEvent.change(within(dialog).getByLabelText(/^Data de vigência/), {
        target: { value: '2026-09-01T00:00' },
      });
      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), { target: { value: 'Teste' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Confirmar mudança' }));

      expect(
        await within(dialog).findByText('Plano comercial não disponível.'),
      ).toBeInTheDocument();
      expect(screen.getByRole('dialog', { name: 'Alterar plano' })).toBeInTheDocument();
    });

    it('botão "Cancelar" fecha o diálogo sem chamar a API', async () => {
      const update = vi.fn();
      const api = buildApi({ update });
      render(<TenantPlanSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Alterar plano/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Alterar plano' });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Cancelar' }));

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(update).not.toHaveBeenCalled();
    });
  });
});
