// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type { AttentionQueueItem, AttentionQueueListResponse } from '@nexora/contracts';
import type { PlatformAttentionApi } from './platform-attention-api.js';
import { PlatformAttentionPage } from './platform-attention-page.js';

const TENANT_ID = '0198aabb-0001-7000-8000-000000000001';

const offlineItem: AttentionQueueItem = {
  id: 'INSTALLATION_OFFLINE|' + TENANT_ID + '|0198aabb-0002-7000-8000-000000000001',
  tenantId: TENANT_ID,
  tenantName: 'Pizzaria Dona Betinha',
  type: 'INSTALLATION_OFFLINE',
  severity: 'CRITICAL',
  since: '2026-08-06T10:00:00Z',
  reason: 'Sem contato há 18 min',
  action: { kind: 'OPEN_DIAGNOSTICS', href: '/instalacoes' },
};

const inviteItem: AttentionQueueItem = {
  id: 'INVITE_EXPIRED|' + TENANT_ID + '|0198aabb-0003-7000-8000-000000000001',
  tenantId: TENANT_ID,
  tenantName: 'Pizzaria do Zé',
  type: 'INVITE_EXPIRED',
  severity: 'MEDIUM',
  since: '2026-08-01T10:00:00Z',
  reason: 'Convite expirado há 5 dias, proprietário ainda sem acesso',
  action: { kind: 'OPEN_TENANT', href: '/estabelecimentos/' + TENANT_ID },
};

function buildResponse(
  data: AttentionQueueItem[],
  overrides: Partial<AttentionQueueListResponse> = {},
): AttentionQueueListResponse {
  return {
    data,
    nextCursor: null,
    meta: { collectedAt: '2026-08-06T10:20:00Z', unavailableSources: [] },
    ...overrides,
  };
}

function buildApi(overrides: Partial<PlatformAttentionApi> = {}): PlatformAttentionApi {
  return {
    list: vi.fn().mockResolvedValue(buildResponse([offlineItem, inviteItem])),
    acknowledge: vi.fn().mockResolvedValue({
      id: 'ack-1',
      itemId: offlineItem.id,
      reason: 'Cliente avisado.',
      acknowledgedAt: '2026-08-06T10:21:00Z',
    }),
    exportCsv: vi.fn().mockResolvedValue(new Blob(['csv'], { type: 'text/csv' })),
    ...overrides,
  };
}

describe('PlatformAttentionPage', () => {
  it('mostra a fila priorizada com severidade, motivo e tempo na condição (Gherkin "Priorização explicável")', async () => {
    const api = buildApi();
    const navigate = vi.fn();
    render(
      <PlatformAttentionPage api={api} navigate={navigate} onRequestSupportAccess={vi.fn()} />,
    );

    expect(await screen.findByText('Pizzaria Dona Betinha')).toBeInTheDocument();
    expect(screen.getByText('Sem contato há 18 min')).toBeInTheDocument();
    expect(screen.getByText('Pizzaria do Zé')).toBeInTheDocument();
    expect(
      screen.getByText('Convite expirado há 5 dias, proprietário ainda sem acesso'),
    ).toBeInTheDocument();
    // Não esconde itens menos graves — os dois aparecem juntos.
    expect(screen.getAllByRole('listitem')).toHaveLength(2);
  });

  it('mostra o horário da última coleta', async () => {
    const api = buildApi();
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    expect(screen.getByText(/Última coleta:/)).toBeInTheDocument();
  });

  it('Gherkin "Falha parcial": fonte indisponível mostra aviso sem esconder os dados disponíveis', async () => {
    const api = buildApi({
      list: vi.fn().mockResolvedValue(
        buildResponse([inviteItem], {
          meta: {
            collectedAt: '2026-08-06T10:20:00Z',
            unavailableSources: ['INSTALLATION_HEALTH'],
          },
        }),
      ),
    });
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    expect(await screen.findByText('Pizzaria do Zé')).toBeInTheDocument();
    expect(
      screen.getByText(/Fontes indisponíveis nesta coleta: INSTALLATION_HEALTH/),
    ).toBeInTheDocument();
  });

  it('clicar em "Ver diagnóstico" navega para o href retornado pelo backend', async () => {
    const api = buildApi();
    const navigate = vi.fn();
    render(
      <PlatformAttentionPage api={api} navigate={navigate} onRequestSupportAccess={vi.fn()} />,
    );

    fireEvent.click(await screen.findByRole('button', { name: 'Ver diagnóstico' }));
    expect(navigate).toHaveBeenCalledWith('/instalacoes');
  });

  it('Gherkin "Atalho de suporte": pedir suporte encaminha ao fluxo autorizado, sem criar token silenciosamente', async () => {
    const api = buildApi();
    const onRequestSupportAccess = vi.fn();
    render(
      <PlatformAttentionPage
        api={api}
        navigate={vi.fn()}
        onRequestSupportAccess={onRequestSupportAccess}
      />,
    );

    fireEvent.click(await screen.findByRole('button', { name: /Solicitar suporte/ }));

    expect(onRequestSupportAccess).toHaveBeenCalledWith(TENANT_ID);
    expect(api.acknowledge).not.toHaveBeenCalled();
  });

  it('reconhecer exige motivo e não é um atalho de um clique (ação não trivialmente destrutiva mesmo assim passa por confirmação)', async () => {
    const api = buildApi();
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    fireEvent.click((await screen.findAllByRole('button', { name: 'Reconhecer' }))[0]!);

    const dialog = await screen.findByRole('dialog');
    const confirmButton = within(dialog).getByRole('button', { name: 'Confirmar reconhecimento' });
    expect(confirmButton).toBeDisabled();

    fireEvent.change(within(dialog).getByLabelText(/Motivo/), {
      target: { value: 'Cliente avisado.' },
    });
    expect(confirmButton).toBeEnabled();

    fireEvent.click(confirmButton);

    await waitFor(() =>
      expect(api.acknowledge).toHaveBeenCalledWith(offlineItem.id, { reason: 'Cliente avisado.' }),
    );
    await waitFor(() =>
      expect(screen.queryByText('Sem contato há 18 min')).not.toBeInTheDocument(),
    );
  });

  it('exportar CSV dispara o download sem quebrar a tela', async () => {
    const api = buildApi();
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    fireEvent.click(screen.getByRole('button', { name: /Exportar CSV/ }));

    await waitFor(() => expect(api.exportCsv).toHaveBeenCalledTimes(1));
  });

  it('sem pendências mostra estado vazio', async () => {
    const api = buildApi({ list: vi.fn().mockResolvedValue(buildResponse([])) });
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    expect(await screen.findByText('Nenhuma pendência no momento')).toBeInTheDocument();
  });

  it('erro ao carregar mostra estado de erro', async () => {
    const api = buildApi({ list: vi.fn().mockRejectedValue(new Error('Falha de rede')) });
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
  });

  it('filtrar por severidade reconsulta a API com o filtro aplicado', async () => {
    const api = buildApi();
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    fireEvent.click(screen.getByRole('button', { name: 'Crítica' }));

    await waitFor(() =>
      expect(api.list).toHaveBeenLastCalledWith(
        expect.objectContaining({ severity: ['CRITICAL'] }),
      ),
    );
  });

  it('carregar mais envia o cursor recebido e acrescenta a próxima página sem duplicar a primeira', async () => {
    const list = vi
      .fn()
      .mockResolvedValueOnce(
        buildResponse([offlineItem], { nextCursor: 'cursor-da-primeira-pagina' }),
      )
      .mockResolvedValueOnce(buildResponse([inviteItem]));
    const api = buildApi({ list });
    render(<PlatformAttentionPage api={api} navigate={vi.fn()} onRequestSupportAccess={vi.fn()} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Carregar mais' }));

    await waitFor(() =>
      expect(list).toHaveBeenNthCalledWith(
        2,
        expect.objectContaining({ cursor: 'cursor-da-primeira-pagina' }),
      ),
    );
    expect(screen.getAllByRole('listitem')).toHaveLength(2);
    expect(screen.getByText('Pizzaria Dona Betinha')).toBeInTheDocument();
    expect(screen.getByText('Pizzaria do Zé')).toBeInTheDocument();
  });
});
