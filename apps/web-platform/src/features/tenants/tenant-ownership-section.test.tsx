// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type {
  ApiProblem,
  CreateOwnerInviteResult,
  TenantOwnershipApi,
  TenantOwnershipInvite,
  TenantOwnershipTransferHistory,
  TenantOwnershipView,
  TransferTenantOwnershipResult,
  UnlockOwnerAccessResult,
} from './tenant-ownership-api.js';
import { TenantOwnershipSection } from './tenant-ownership-section.js';

const TENANT_ID = '0198aabb-0002-7000-8000-000000000001';

const pendingInvite: TenantOwnershipInvite = {
  id: '0198aabb-0002-7000-8000-000000000010',
  sentTo: 'dona.betinha@example.com',
  status: 'PENDING',
  deliveryStatus: 'SENT',
  createdAt: '2026-08-01T10:00:00Z',
  expiresAt: '2026-08-04T10:00:00Z',
  consumedAt: null,
  revokedAt: null,
  revokedReason: null,
  reason: null,
};

const revokedInvite: TenantOwnershipInvite = {
  ...pendingInvite,
  id: '0198aabb-0002-7000-8000-000000000011',
  status: 'REVOKED',
  deliveryStatus: 'UNKNOWN',
  revokedAt: '2026-08-02T10:00:00Z',
  revokedReason: 'Endereço incorreto',
};

const transferHistory: TenantOwnershipTransferHistory = {
  id: '0198aabb-0002-7000-8000-000000000020',
  previousOwnerUserId: '0198aabb-0002-7000-8000-000000000030',
  newOwnerUserId: '0198aabb-0002-7000-8000-000000000031',
  reason: 'Alteração societária',
  previousKeptAsAdmin: false,
  transferredAt: '2026-07-01T10:00:00Z',
};

function buildView(overrides: Partial<TenantOwnershipView> = {}): TenantOwnershipView {
  return {
    owner: {
      id: '0198aabb-0002-7000-8000-000000000030',
      name: 'Dona Betinha',
      email: 'dona.betinha@example.com',
      status: 'INVITED',
    },
    invites: [pendingInvite],
    transfers: [],
    ...overrides,
  };
}

function buildApi(overrides: Partial<TenantOwnershipApi> = {}): TenantOwnershipApi {
  return {
    get: vi.fn().mockResolvedValue(buildView()),
    createInvite: vi.fn().mockResolvedValue({
      inviteId: '0198aabb-0002-7000-8000-000000000099',
      sentTo: 'dona.betinha@example.com',
      expiresAt: '2026-08-07T10:00:00Z',
    } satisfies CreateOwnerInviteResult),
    revokeInvite: vi.fn().mockResolvedValue(undefined),
    transferOwnership: vi.fn().mockResolvedValue({
      previousOwnerUserId: transferHistory.previousOwnerUserId,
      newOwnerUserId: transferHistory.newOwnerUserId,
      previousKeptAsAdmin: false,
      transferredAt: '2026-08-07T10:00:00Z',
    } satisfies TransferTenantOwnershipResult),
    unlock: vi.fn().mockResolvedValue({ userId: '0198aabb-0002-7000-8000-000000000030', status: 'ACTIVE' } satisfies UnlockOwnerAccessResult),
    ...overrides,
  };
}

function genericError(): ApiProblem {
  return new Error('Falha de rede') as ApiProblem;
}

describe('TenantOwnershipSection', () => {
  it('mostra nome, e-mail mascarado e estado do proprietário', async () => {
    const api = buildApi();
    render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Dona Betinha')).toBeInTheDocument();
    // "d***@example.com" aparece duas vezes (resumo do dono + linha do convite na tabela abaixo).
    expect(screen.getAllByText('d***@example.com').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Convidado')).toBeInTheDocument();
    expect(api.get).toHaveBeenCalledWith(TENANT_ID);
  });

  it('nunca exibe token bruto ou hash em lugar nenhum da tela', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ invites: [pendingInvite, revokedInvite] })) });
    render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

    await screen.findByText('Dona Betinha');

    const html = document.body.innerHTML.toLowerCase();
    expect(html).not.toContain('hash');
    expect(html).not.toContain('secrethash');
  });

  it('sem proprietário mostra badge "Sem proprietário" e nenhum botão de transferência', async () => {
    const api = buildApi({
      get: vi.fn().mockResolvedValue(buildView({ owner: { id: null, name: null, email: null, status: 'NONE' }, invites: [] })),
    });
    render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Sem proprietário')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Transferir titularidade/ })).not.toBeInTheDocument();
  });

  it('histórico de convites mostra status, entrega e motivo de revogação', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ invites: [pendingInvite, revokedInvite] })) });
    render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Pendente')).toBeInTheDocument();
    expect(screen.getByText('Revogado')).toBeInTheDocument();
    expect(screen.getByText('Entregue')).toBeInTheDocument();
    expect(screen.getByText('Endereço incorreto')).toBeInTheDocument();
  });

  it('histórico de transferências aparece quando existente', async () => {
    const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ transfers: [transferHistory] })) });
    render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Alteração societária')).toBeInTheDocument();
  });

  it('erro ao carregar mostra AlertBanner de perigo', async () => {
    const api = buildApi({ get: vi.fn().mockRejectedValue(genericError()) });
    render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
  });

  describe('Reenviar/corrigir convite', () => {
    it('só aparece quando o proprietário está CONVIDADO', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ owner: { id: '1', name: 'X', email: 'x@example.com', status: 'ACTIVE' } })) });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      await screen.findByText('Ativo');
      expect(screen.queryByRole('button', { name: /Reenviar\/corrigir convite/ })).not.toBeInTheDocument();
    });

    it('avisa que o link anterior deixa de funcionar antes de confirmar', async () => {
      const api = buildApi();
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Reenviar\/corrigir convite/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Reenviar ou corrigir convite' });

      expect(within(dialog).getByText('O link anterior deixa de funcionar')).toBeInTheDocument();
    });

    it('confirma reenvio/correção com nome, e-mail e motivo, e recarrega a lista', async () => {
      const get = vi.fn().mockResolvedValueOnce(buildView()).mockResolvedValueOnce(buildView());
      const createInvite = vi.fn().mockResolvedValue({
        inviteId: 'new-id',
        sentTo: 'correto@example.com',
        expiresAt: '2026-08-07T10:00:00Z',
      });
      const api = buildApi({ get, createInvite });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Reenviar\/corrigir convite/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Reenviar ou corrigir convite' });

      fireEvent.change(within(dialog).getByLabelText(/^E-mail/), { target: { value: 'correto@example.com' } });
      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), { target: { value: 'Correção solicitada no chamado #91' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Confirmar envio' }));

      await waitFor(() =>
        expect(createInvite).toHaveBeenCalledWith(
          TENANT_ID,
          expect.objectContaining({ email: 'correto@example.com', reason: 'Correção solicitada no chamado #91' }),
        ),
      );
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(get).toHaveBeenCalledTimes(2);
    });
  });

  describe('Revogar convite', () => {
    it('botão "Revogar" só aparece para convite pendente', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ invites: [pendingInvite, revokedInvite] })) });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      await screen.findByText('Pendente');
      expect(screen.getAllByRole('button', { name: 'Revogar' })).toHaveLength(1);
    });

    it('confirma revogação com motivo e recarrega a lista', async () => {
      const get = vi.fn().mockResolvedValueOnce(buildView()).mockResolvedValueOnce(buildView({ invites: [revokedInvite] }));
      const revokeInvite = vi.fn().mockResolvedValue(undefined);
      const api = buildApi({ get, revokeInvite });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: 'Revogar' }));
      const dialog = await screen.findByRole('dialog', { name: 'Revogar convite pendente?' });

      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), { target: { value: 'Convite não é mais necessário' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, revogar convite' }));

      await waitFor(() => expect(revokeInvite).toHaveBeenCalledWith(TENANT_ID, pendingInvite.id, 'Convite não é mais necessário'));
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });
  });

  describe('Transferir titularidade', () => {
    it('explica o que o antigo proprietário mantém em cada opção de "manter como admin"', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ owner: { id: '1', name: 'Dona Betinha', email: 'x@example.com', status: 'ACTIVE' } })) });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Transferir titularidade/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Transferir titularidade' });

      expect(within(dialog).getByText(/mantém um papel administrativo equivalente/)).toBeInTheDocument();
    });

    it('exige novo proprietário e motivo antes de confirmar', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ owner: { id: '1', name: 'Dona Betinha', email: 'x@example.com', status: 'ACTIVE' } })) });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Transferir titularidade/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Transferir titularidade' });
      const confirmButton = within(dialog).getByRole('button', { name: 'Confirmar transferência' });

      expect(confirmButton).toBeDisabled();

      fireEvent.change(within(dialog).getByLabelText(/ID do novo proprietário/), {
        target: { value: transferHistory.newOwnerUserId },
      });
      expect(confirmButton).toBeDisabled();

      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), { target: { value: 'Alteração societária' } });
      expect(confirmButton).toBeEnabled();
    });

    it('confirma a transferência e recarrega', async () => {
      const get = vi
        .fn()
        .mockResolvedValueOnce(buildView({ owner: { id: '1', name: 'Dona Betinha', email: 'x@example.com', status: 'ACTIVE' } }))
        .mockResolvedValueOnce(buildView({ owner: { id: '2', name: 'Novo Dono', email: 'novo@example.com', status: 'ACTIVE' } }));
      const transferOwnership = vi.fn().mockResolvedValue({
        previousOwnerUserId: '1',
        newOwnerUserId: transferHistory.newOwnerUserId,
        previousKeptAsAdmin: false,
        transferredAt: '2026-08-07T10:00:00Z',
      });
      const api = buildApi({ get, transferOwnership });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Transferir titularidade/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Transferir titularidade' });

      fireEvent.change(within(dialog).getByLabelText(/ID do novo proprietário/), {
        target: { value: transferHistory.newOwnerUserId },
      });
      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), { target: { value: 'Alteração societária' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Confirmar transferência' }));

      await waitFor(() =>
        expect(transferOwnership).toHaveBeenCalledWith(TENANT_ID, {
          newOwnerUserId: transferHistory.newOwnerUserId,
          reason: 'Alteração societária',
          keepPreviousAsAdmin: false,
        }),
      );
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(await screen.findByText('Novo Dono')).toBeInTheDocument();
    });
  });

  describe('Desbloqueio administrativo', () => {
    it('botão só aparece quando o proprietário está bloqueado', async () => {
      const api = buildApi({ get: vi.fn().mockResolvedValue(buildView({ owner: { id: '1', name: 'X', email: 'x@example.com', status: 'ACTIVE' } })) });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      await screen.findByText('Ativo');
      expect(screen.queryByRole('button', { name: /Desbloquear acesso/ })).not.toBeInTheDocument();
    });

    it('confirma desbloqueio e nunca envia/recebe valor de senha', async () => {
      const get = vi
        .fn()
        .mockResolvedValueOnce(buildView({ owner: { id: '1', name: 'Dona Betinha', email: 'x@example.com', status: 'BLOCKED' } }))
        .mockResolvedValueOnce(buildView({ owner: { id: '1', name: 'Dona Betinha', email: 'x@example.com', status: 'ACTIVE' } }));
      const unlock = vi.fn().mockResolvedValue({ userId: '1', status: 'ACTIVE' });
      const api = buildApi({ get, unlock });
      render(<TenantOwnershipSection tenantId={TENANT_ID} api={api} />);

      fireEvent.click(await screen.findByRole('button', { name: /Desbloquear acesso/ }));
      const dialog = await screen.findByRole('dialog', { name: 'Desbloquear acesso do proprietário?' });

      fireEvent.change(within(dialog).getByLabelText(/^Motivo/), { target: { value: 'Chamado de suporte #12' } });
      fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, desbloquear' }));

      // unlock() só recebe (tenantId, reason) — a assinatura do cliente HTTP nem permite passar
      // uma senha; o resultado tipado (UnlockOwnerAccessResult) também não tem campo de senha.
      await waitFor(() => expect(unlock).toHaveBeenCalledWith(TENANT_ID, 'Chamado de suporte #12'));
      expect(unlock).toHaveBeenCalledTimes(1);
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });
  });
});
