// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type { TenantOverviewResponse, TenantStatusTransitionResponse } from '@nexora/contracts';
import type { ApiProblem, TenantOverviewApi } from './tenant-detail-api.js';
import { TenantDetailPage } from './tenant-detail-page.js';

const TENANT_ID = '0198aabb-0001-7000-8000-000000000001';

const healthyOverview: TenantOverviewResponse = {
  tenant: {
    id: TENANT_ID,
    name: 'Pizzaria Dona Betinha',
    slug: 'dona-betinha',
    status: 'ACTIVE',
    statusVersion: 5,
    availableTransitions: ['SUSPENDED', 'CANCELLED'],
    plan: 'COMPLETO',
    template: 'PIZZERIA',
    domain: 'donabetinha.nexora.app',
    createdAt: '2026-01-10T12:00:00Z',
    updatedAt: '2026-02-01T09:00:00Z',
  },
  owner: { name: 'Betinha Silva', email: 'betinha@example.com', inviteStatus: 'ACCEPTED' },
  stores: [{ id: '0198aabb-0002-7000-8000-000000000001', name: 'Matriz', timezone: 'America/Sao_Paulo' }],
  installations: [
    { id: '0198aabb-0003-7000-8000-000000000001', label: 'Matriz — edge', status: 'ACTIVE', health: 'OK' },
  ],
  deployment: { completed: 9, total: 9, nextAction: null },
  links: { publicMenu: 'https://donabetinha.nexora.app/cardapio', admin: 'https://admin.donabetinha.nexora.app', health: null },
};

const incompleteOverview: TenantOverviewResponse = {
  ...healthyOverview,
  tenant: { ...healthyOverview.tenant, status: 'INSTALLING', availableTransitions: [] },
  owner: { name: 'Betinha Silva', email: 'betinha@example.com', inviteStatus: 'PENDING' },
  installations: [
    { id: '0198aabb-0003-7000-8000-000000000001', label: 'Matriz — edge', status: 'PENDING', health: 'UNKNOWN' },
  ],
  deployment: { completed: 4, total: 9, nextAction: 'EDGE_INSTALL' },
};

const suspendedOverview: TenantOverviewResponse = {
  ...healthyOverview,
  tenant: { ...healthyOverview.tenant, status: 'SUSPENDED', availableTransitions: ['ACTIVE', 'CANCELLED'] },
};

const cancelledOverview: TenantOverviewResponse = {
  ...healthyOverview,
  tenant: { ...healthyOverview.tenant, status: 'CANCELLED', availableTransitions: [] },
};

const transitionResponse: TenantStatusTransitionResponse = {
  tenantId: TENANT_ID,
  previousStatus: 'ACTIVE',
  status: 'SUSPENDED',
  version: 6,
  changedAt: '2026-08-05T12:00:00Z',
};

function buildApi(overrides: Partial<TenantOverviewApi> = {}): TenantOverviewApi {
  return {
    get: vi.fn().mockResolvedValue(healthyOverview),
    transitionStatus: vi.fn().mockResolvedValue(transitionResponse),
    ...overrides,
  };
}

function notFoundError(): ApiProblem {
  const error = new Error('Estabelecimento não encontrado.') as ApiProblem;
  error.status = 404;
  error.code = 'TENANT_NOT_FOUND';
  return error;
}

describe('TenantDetailPage', () => {
  it('cadastro saudável: mostra cabeçalho, proprietário, lojas, instalações e checklist concluído (US-152 §4)', async () => {
    const api = buildApi();
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    expect(await screen.findByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeInTheDocument();
    expect(screen.getByText('dona-betinha')).toBeInTheDocument();
    expect(screen.getByText('Ativo')).toBeInTheDocument();
    // "COMPLETO" aparece duas vezes de propósito (Cabeçalho e Resumo de branding/configuração).
    expect(screen.getAllByText('COMPLETO').length).toBeGreaterThan(0);
    expect(screen.getByText('Betinha Silva')).toBeInTheDocument();
    expect(screen.getByText('Convite aceito')).toBeInTheDocument();
    expect(screen.getByText('Matriz')).toBeInTheDocument();
    expect(screen.getByText('America/Sao_Paulo')).toBeInTheDocument();
    expect(screen.getByText('Matriz — edge')).toBeInTheDocument();
    expect(screen.getByText('9/9')).toBeInTheDocument();
    expect(screen.getByText('Implantação concluída')).toBeInTheDocument();
    expect(screen.queryByText('Próxima ação recomendada')).not.toBeInTheDocument();
    expect(api.get).toHaveBeenCalledWith(TENANT_ID);
  });

  it('provisionamento incompleto: instalação pendente e banner com a próxima ação em PT-BR (US-152 §4)', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(incompleteOverview) });
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    expect(await screen.findByText('Pendente')).toBeInTheDocument();
    expect(screen.getByText('Desconhecida')).toBeInTheDocument();
    expect(screen.getByText('4/9')).toBeInTheDocument();
    expect(screen.getByText('Próxima ação recomendada')).toBeInTheDocument();
    // ONBOARDING_STEP_LABELS.EDGE_INSTALL
    expect(screen.getByText(/Servidor local instalado/)).toBeInTheDocument();
  });

  it('clicar em "Solicitar acesso de suporte" aciona o fluxo da US-145 e a tela nunca mostra dado de negócio (US-152 §3.2/§4)', async () => {
    const onRequestSupportAccess = vi.fn();
    const api = buildApi();
    const { container } = render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={onRequestSupportAccess}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    const button = await screen.findByRole('button', { name: /Solicitar acesso de suporte/ });
    fireEvent.click(button);

    expect(onRequestSupportAccess).toHaveBeenCalledWith(TENANT_ID);
    expect(onRequestSupportAccess).toHaveBeenCalledTimes(1);

    // RN-015: nunca exibe pedido, caixa, estoque ou financeiro do cliente — nem como link, nem como número.
    const text = container.textContent ?? '';
    for (const forbidden of ['pedido', 'Pedido', 'caixa', 'Caixa', 'estoque', 'Estoque', 'financeiro', 'Financeiro']) {
      expect(text).not.toContain(forbidden);
    }
  });

  it('recurso inexistente: 404 mostra estado claro de "não encontrado", nunca um erro genérico (US-152 §4)', async () => {
    const api = buildApi({ get: vi.fn().mockRejectedValue(notFoundError()) });
    render(
      <TenantDetailPage
        tenantId="tenant-inexistente"
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    expect(await screen.findByText('Estabelecimento não encontrado')).toBeInTheDocument();
    expect(screen.queryByText('Estabelecimento não encontrado.')).not.toBeInTheDocument();
  });

  it('erro genérico mostra AlertBanner de perigo', async () => {
    const api = buildApi({ get: vi.fn().mockRejectedValue(new Error('Falha de rede')) });
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
  });

  it('onBack é acionado pelo botão de voltar', async () => {
    const onBack = vi.fn();
    const api = buildApi();
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={onBack}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    const button = await screen.findByRole('button', { name: /Voltar ao diretório/ });
    fireEvent.click(button);
    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('onOpenInstallations é acionado pelo link interno de instalações, nunca externo', async () => {
    const onOpenInstallations = vi.fn();
    const api = buildApi();
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={onOpenInstallations}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    const button = await screen.findByRole('button', { name: /Ver instalações/ });
    fireEvent.click(button);
    expect(onOpenInstallations).toHaveBeenCalledTimes(1);
  });

  it('onOpenBusinessTemplates é acionado pelo atalho de resumo de branding', async () => {
    const onOpenBusinessTemplates = vi.fn();
    const api = buildApi();
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={onOpenBusinessTemplates}
      />,
    );

    const button = await screen.findByRole('button', { name: /Ver modelos de negócio/ });
    fireEvent.click(button);
    expect(onOpenBusinessTemplates).toHaveBeenCalledTimes(1);
  });

  it('proprietário ausente mostra "Não configurado" em vez de seção vazia', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue({ ...healthyOverview, owner: null }) });
    render(
      <TenantDetailPage
        tenantId={TENANT_ID}
        api={api}
        onBack={vi.fn()}
        onRequestSupportAccess={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenBusinessTemplates={vi.fn()}
      />,
    );

    expect(await screen.findAllByText('Não configurado')).not.toHaveLength(0);
  });

  describe('Ciclo de vida do estabelecimento (US-153)', () => {
    it('mostra um botão de ação só para cada transição presente em availableTransitions, nunca uma inventada pelo front', async () => {
      const api = buildApi();
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      await screen.findByRole('heading', { name: 'Pizzaria Dona Betinha' });
      // healthyOverview.tenant.availableTransitions === ['SUSPENDED', 'CANCELLED'] — ACTIVE não
      // faz parte do array (não faria sentido "reativar" um tenant já ativo), logo sem botão.
      expect(screen.getByRole('button', { name: 'Suspender' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Cancelar' })).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Reativar' })).not.toBeInTheDocument();
    });

    it('tenant CANCELLED (availableTransitions vazio) não mostra nenhum botão de ação', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(cancelledOverview) });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      await screen.findByRole('heading', { name: 'Pizzaria Dona Betinha' });
      expect(screen.queryByRole('button', { name: 'Suspender' })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Reativar' })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Cancelar' })).not.toBeInTheDocument();
    });

    it('suspender: mostra o impacto, exige motivo, envia If-Match com o statusVersion corrente e atualiza após sucesso (US-153 §4/§10)', async () => {
      const transitionStatus = vi.fn().mockResolvedValue(transitionResponse);
      const get = vi.fn().mockResolvedValueOnce(healthyOverview).mockResolvedValueOnce(suspendedOverview);
      const api = buildApi({ get, transitionStatus });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Suspender' }));

      const dialog = await screen.findByRole('dialog', { name: 'Suspender estabelecimento?' });
      expect(
        within(dialog).getByText(
          'Novas sessões de gestão e operação seguirão a política de suspensão até a reativação.',
        ),
      ).toBeInTheDocument();

      const confirmButton = within(dialog).getByRole('button', { name: 'Sim, suspender estabelecimento' });
      expect(confirmButton).toBeDisabled();

      fireEvent.change(within(dialog).getByLabelText('Motivo'), {
        target: { value: 'Inadimplência — chamado #482' },
      });
      expect(confirmButton).toBeEnabled();

      fireEvent.click(confirmButton);

      await waitFor(() =>
        expect(transitionStatus).toHaveBeenCalledWith(TENANT_ID, 5, {
          targetStatus: 'SUSPENDED',
          reason: 'Inadimplência — chamado #482',
        }),
      );
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(get).toHaveBeenCalledTimes(2);
      expect(await screen.findByText('Suspenso')).toBeInTheDocument();
    });

    it('motivo em branco (só espaços) mantém a confirmação desabilitada', async () => {
      const api = buildApi();
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Suspender' }));
      const dialog = await screen.findByRole('dialog');
      const confirmButton = within(dialog).getByRole('button', { name: 'Sim, suspender estabelecimento' });

      fireEvent.change(within(dialog).getByLabelText('Motivo'), { target: { value: '   ' } });
      expect(confirmButton).toBeDisabled();
    });

    it('cancelar exige digitar o slug do estabelecimento exatamente antes de habilitar a confirmação (US-153 §10)', async () => {
      const transitionStatus = vi.fn().mockResolvedValue({
        tenantId: TENANT_ID,
        previousStatus: 'ACTIVE',
        status: 'CANCELLED',
        version: 6,
        changedAt: '2026-08-05T12:00:00Z',
      });
      const get = vi.fn().mockResolvedValueOnce(healthyOverview).mockResolvedValueOnce(cancelledOverview);
      const api = buildApi({ get, transitionStatus });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Cancelar' }));

      const dialog = await screen.findByRole('dialog', { name: 'Cancelar estabelecimento?' });
      expect(
        within(dialog).getByText(
          'Esta ação é permanente. O estabelecimento e seus dados de histórico são preservados, mas o encerramento não pode ser desfeito.',
        ),
      ).toBeInTheDocument();

      const confirmButton = within(dialog).getByRole('button', { name: 'Sim, cancelar estabelecimento' });
      fireEvent.change(within(dialog).getByLabelText('Motivo'), { target: { value: 'Encerramento contratual' } });
      // Motivo preenchido, mas o slug de confirmação ainda não — continua desabilitado.
      expect(confirmButton).toBeDisabled();

      const slugInput = within(dialog).getByLabelText('Digite "dona-betinha" para confirmar');
      fireEvent.change(slugInput, { target: { value: 'dona-bet' } });
      expect(confirmButton).toBeDisabled();

      fireEvent.change(slugInput, { target: { value: 'dona-betinha' } });
      expect(confirmButton).toBeEnabled();

      fireEvent.click(confirmButton);

      await waitFor(() =>
        expect(transitionStatus).toHaveBeenCalledWith(TENANT_ID, 5, {
          targetStatus: 'CANCELLED',
          reason: 'Encerramento contratual',
        }),
      );
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });

    it('REASON_REQUIRED do servidor aparece no AlertBanner e mantém o modal aberto', async () => {
      const error = new Error('Informe um motivo para a transição.') as ApiProblem;
      error.code = 'REASON_REQUIRED';
      error.status = 422;
      const api = buildApi({ transitionStatus: vi.fn().mockRejectedValue(error) });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Suspender' }));
      const dialog = await screen.findByRole('dialog');
      fireEvent.change(within(dialog).getByLabelText('Motivo'), { target: { value: 'Motivo válido' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, suspender estabelecimento' }));

      expect(await within(dialog).findByText('Informe um motivo para a transição.')).toBeInTheDocument();
      expect(screen.getByRole('dialog', { name: 'Suspender estabelecimento?' })).toBeInTheDocument();
    });

    it('TENANT_STATUS_TRANSITION_INVALID do servidor aparece no AlertBanner e mantém o modal aberto', async () => {
      const error = new Error('Esta transição não é permitida para o status atual.') as ApiProblem;
      error.code = 'TENANT_STATUS_TRANSITION_INVALID';
      error.status = 409;
      const api = buildApi({ transitionStatus: vi.fn().mockRejectedValue(error) });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Suspender' }));
      const dialog = await screen.findByRole('dialog');
      fireEvent.change(within(dialog).getByLabelText('Motivo'), { target: { value: 'Motivo válido' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, suspender estabelecimento' }));

      expect(await within(dialog).findByText('Esta transição não é permitida para o status atual.')).toBeInTheDocument();
      expect(screen.getByRole('dialog', { name: 'Suspender estabelecimento?' })).toBeInTheDocument();
    });

    it('CONCURRENCY_CONFLICT mostra a dica de recarregar e mantém o modal aberto (US-153 §4 cenário "Concorrência")', async () => {
      const error = new Error('Version mismatch.') as ApiProblem;
      error.code = 'CONCURRENCY_CONFLICT';
      error.status = 409;
      const api = buildApi({ transitionStatus: vi.fn().mockRejectedValue(error) });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Suspender' }));
      const dialog = await screen.findByRole('dialog');
      fireEvent.change(within(dialog).getByLabelText('Motivo'), { target: { value: 'Motivo válido' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, suspender estabelecimento' }));

      expect(
        await within(dialog).findByText(/outro administrador já alterou este estabelecimento.*recarregue/i),
      ).toBeInTheDocument();
      expect(screen.getByRole('dialog', { name: 'Suspender estabelecimento?' })).toBeInTheDocument();
    });

    it('botão "Cancelar" do modal fecha o diálogo sem chamar a API', async () => {
      const transitionStatus = vi.fn();
      const api = buildApi({ transitionStatus });
      render(
        <TenantDetailPage
          tenantId={TENANT_ID}
          api={api}
          onBack={vi.fn()}
          onRequestSupportAccess={vi.fn()}
          onOpenInstallations={vi.fn()}
          onOpenBusinessTemplates={vi.fn()}
        />,
      );

      fireEvent.click(await screen.findByRole('button', { name: 'Suspender' }));
      const dialog = await screen.findByRole('dialog');
      fireEvent.click(within(dialog).getByRole('button', { name: 'Cancelar' }));

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(transitionStatus).not.toHaveBeenCalled();
    });
  });
});
