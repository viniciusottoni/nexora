// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { PlatformOverviewPage } from './platform-overview-page.js';

const SUMMARY = {
  tenants: { total: 12, active: 9, attention: 3 },
  installations: { healthy: 8, degraded: 1, offline: 4 },
  pendingInvites: 2,
  generatedAt: '2026-08-05T12:00:00Z',
};

describe('PlatformOverviewPage', () => {
  it('mostra os contadores do resumo da plataforma', () => {
    render(
      <PlatformOverviewPage
        summary={SUMMARY}
        onCreateTenant={vi.fn()}
        onOpenTenants={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenSupportAccess={vi.fn()}
        onOpenAttention={vi.fn()}
      />,
    );

    expect(screen.getByRole('heading', { name: 'Visão geral' })).toBeInTheDocument();
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('9 ativos')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByText('4')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('"Novo estabelecimento" é uma ação, não o conteúdo único da raiz', () => {
    const onCreateTenant = vi.fn();
    render(
      <PlatformOverviewPage
        summary={SUMMARY}
        onCreateTenant={onCreateTenant}
        onOpenTenants={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenSupportAccess={vi.fn()}
        onOpenAttention={vi.fn()}
      />,
    );

    screen.getByRole('button', { name: /Novo estabelecimento/ }).click();
    expect(onCreateTenant).toHaveBeenCalledOnce();
  });

  it('navega para estabelecimentos, instalações, auditoria/suporte e central de atenção pelos atalhos', () => {
    const onOpenTenants = vi.fn();
    const onOpenInstallations = vi.fn();
    const onOpenSupportAccess = vi.fn();
    const onOpenAttention = vi.fn();
    render(
      <PlatformOverviewPage
        summary={SUMMARY}
        onCreateTenant={vi.fn()}
        onOpenTenants={onOpenTenants}
        onOpenInstallations={onOpenInstallations}
        onOpenSupportAccess={onOpenSupportAccess}
        onOpenAttention={onOpenAttention}
      />,
    );

    screen.getByRole('button', { name: 'Ver diretório' }).click();
    screen.getByRole('button', { name: 'Ver instalações' }).click();
    screen.getByRole('button', { name: 'Ver auditoria e suporte' }).click();
    screen.getByRole('button', { name: 'Ver fila de atenção' }).click();

    expect(onOpenTenants).toHaveBeenCalledOnce();
    expect(onOpenInstallations).toHaveBeenCalledOnce();
    expect(onOpenSupportAccess).toHaveBeenCalledOnce();
    expect(onOpenAttention).toHaveBeenCalledOnce();
  });

  it('US-157 — o cartão "Precisam de atenção" também leva direto à fila filtrada', () => {
    const onOpenAttention = vi.fn();
    render(
      <PlatformOverviewPage
        summary={SUMMARY}
        onCreateTenant={vi.fn()}
        onOpenTenants={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenSupportAccess={vi.fn()}
        onOpenAttention={onOpenAttention}
      />,
    );

    screen.getByRole('button', { name: /Precisam de atenção/ }).click();
    expect(onOpenAttention).toHaveBeenCalledOnce();
  });

  it('modo offline (sem resumo) mantém a estrutura visual e avisa que os dados não atualizaram', () => {
    render(
      <PlatformOverviewPage
        summary={undefined}
        onCreateTenant={vi.fn()}
        onOpenTenants={vi.fn()}
        onOpenInstallations={vi.fn()}
        onOpenSupportAccess={vi.fn()}
        onOpenAttention={vi.fn()}
      />,
    );

    expect(screen.getByRole('heading', { name: 'Visão geral' })).toBeInTheDocument();
    expect(screen.getByText(/Sem conexão com a nuvem/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Novo estabelecimento/ })).toBeInTheDocument();
  });
});
