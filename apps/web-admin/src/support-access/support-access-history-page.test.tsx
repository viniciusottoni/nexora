// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { SupportAccessSummary } from '@nexora/contracts';
import { SupportAccessHistoryPage } from './support-access-history-page.js';

const activeGrant: SupportAccessSummary = {
  id: '0198aabb-4444-7000-8000-000000000001',
  tenantId: '0198aabb-4444-7000-8000-00000000000a',
  tenantName: null,
  grantedTo: null,
  reason: 'Investigação de chamado #482',
  durationMinutes: 60,
  grantedAt: '2026-08-04T10:00:00Z',
  expiresAt: '2026-08-04T11:00:00Z',
  revokedAt: null,
  revokedBy: null,
  lastUsedAt: null,
  isActive: true,
};

const revokedGrant: SupportAccessSummary = {
  ...activeGrant,
  id: '0198aabb-4444-7000-8000-000000000002',
  reason: 'Chamado #100',
  durationMinutes: 30,
  revokedAt: '2026-08-04T10:30:00Z',
  isActive: false,
};

afterEach(() => {
  cleanup();
});

describe('SupportAccessHistoryPage', () => {
  it('mostra estado vazio quando nao ha concessoes', () => {
    render(<SupportAccessHistoryPage grants={[]} onRevoke={vi.fn()} />);

    expect(screen.getByText('Nenhum acesso de suporte registrado')).toBeInTheDocument();
  });

  it('lista motivo, duracao e situacao de cada concessao', () => {
    render(<SupportAccessHistoryPage grants={[activeGrant, revokedGrant]} onRevoke={vi.fn()} />);

    expect(screen.getByText('Investigação de chamado #482')).toBeInTheDocument();
    expect(screen.getByText('60 min')).toBeInTheDocument();
    expect(screen.getByText('Ativo')).toBeInTheDocument();
    expect(screen.getByText('Revogado')).toBeInTheDocument();
  });

  it('revoga um acesso ativo', async () => {
    const onRevoke = vi.fn(async () => {});
    render(<SupportAccessHistoryPage grants={[activeGrant]} onRevoke={onRevoke} />);

    fireEvent.click(screen.getByRole('button', { name: 'Revogar' }));

    await waitFor(() => expect(onRevoke).toHaveBeenCalledWith(activeGrant.id));
    await screen.findByText('Acesso de suporte revogado. Ele deixa de valer imediatamente.');
  });

  it('nao mostra botao de revogar para acesso ja revogado', () => {
    render(<SupportAccessHistoryPage grants={[revokedGrant]} onRevoke={vi.fn()} />);

    expect(screen.queryByRole('button', { name: 'Revogar' })).not.toBeInTheDocument();
  });

  it('mostra erro quando a revogacao falha', async () => {
    const onRevoke = vi.fn(async () => {
      throw new Error('Acesso de suporte não encontrado.');
    });
    render(<SupportAccessHistoryPage grants={[activeGrant]} onRevoke={onRevoke} />);

    fireEvent.click(screen.getByRole('button', { name: 'Revogar' }));

    await screen.findByText('Acesso de suporte não encontrado.');
  });
});
