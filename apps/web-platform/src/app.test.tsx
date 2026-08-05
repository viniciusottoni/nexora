// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from './app.js';

const ACCESS_KEY = 'food-operations.cloud.access';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const SUMMARY_BODY = {
  tenants: { total: 5, active: 4, attention: 1 },
  installations: { healthy: 3, degraded: 0, offline: 1 },
  pendingInvites: 1,
  generatedAt: '2026-08-05T12:00:00Z',
};

describe('App (US-150 — estrutura e navegação do painel de plataforma)', () => {
  beforeEach(() => {
    window.history.pushState({}, '', '/');
  });

  afterEach(() => {
    // US-152 — o novo teste desta suíte faz clique + troca de rota (navegação real dentro do
    // shell), o primeiro fluxo multi-etapa daqui; sem desmontar explicitamente a árvore React (o
    // `afterEach` global só limpa `body.innerHTML`, nunca chama `unmount`), o efeito de
    // `PlatformAdminGate`/`usePathname` seguia "montado" e um contêiner órfão sobrevivia para o
    // teste seguinte, fazendo `getByRole` achar dois shells simultâneos. `cleanup()` desmonta de
    // verdade (roda os `return () => {...}` dos efeitos) antes da limpeza manual do body.
    cleanup();
    localStorage.clear();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('sem sessão, mostra a tela de login (nunca o shell)', () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: 'Entrar na gestão' })).toBeInTheDocument();
  });

  it('entrada pela raiz: visão geral com navegação para Estabelecimentos, Instalações e Auditoria e suporte; "Novo estabelecimento" é uma ação', async () => {
    localStorage.setItem(ACCESS_KEY, 'token-admin');
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(SUMMARY_BODY)));

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Visão geral' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Estabelecimentos' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Instalações' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Auditoria e suporte' })).toBeInTheDocument();
    // A raiz mostra a visão geral, e "Novo estabelecimento" é uma AÇÃO — não o conteúdo da rota.
    expect(screen.queryByRole('heading', { name: 'Provisionar estabelecimento' })).not.toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /Novo estabelecimento/ }).length).toBeGreaterThan(0);
  });

  it('usuário sem policy PlatformAdmin recebe acesso negado, sem nenhum dado de plataforma', async () => {
    localStorage.setItem(ACCESS_KEY, 'token-sem-permissao');
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => jsonResponse({ detail: 'Acesso negado.', code: 'FORBIDDEN' }, 403)),
    );
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Acesso negado' })).toBeInTheDocument();
    expect(screen.queryByText(String(SUMMARY_BODY.tenants.total))).not.toBeInTheDocument();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
    expect(warn).toHaveBeenCalled();
  });

  it('acesso direto a uma rota protegida (deep link) renderiza a rota com o shell, mantendo o caminho após reload', async () => {
    localStorage.setItem(ACCESS_KEY, 'token-admin');
    window.history.pushState({}, '', '/instalacoes');
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(SUMMARY_BODY)));

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Instalações' })).toBeInTheDocument();
    const activeItem = screen.getByRole('button', { name: 'Instalações' });
    expect(activeItem).toHaveAttribute('aria-current', 'page');
    expect(window.location.pathname).toBe('/instalacoes');
  });

  it('sessão expirada (401 na sonda) encerra a sessão local e volta ao login sem loop de redirecionamento', async () => {
    localStorage.setItem(ACCESS_KEY, 'token-expirado');
    // Sem refresh token: authenticatedFetch (packages/ui) não tenta renovar, devolve o 401 direto.
    const fetchMock = vi.fn(async (_input: RequestInfo | URL) => jsonResponse({ detail: 'Sessão expirada.' }, 401));
    vi.stubGlobal('fetch', fetchMock);

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Entrar na gestão' })).toBeInTheDocument();
    expect(localStorage.getItem(ACCESS_KEY)).toBeNull();
    // Uma única chamada à sonda de autorização — nenhuma repetição (sem loop de redirecionamento).
    // (a 2ª chamada registrada é o OTP de desenvolvimento do próprio CloudLoginScreen, já visível
    // na tela de login mostrada acima — nada relacionado à sonda de `/v1/platform/summary`.)
    const summaryCalls = fetchMock.mock.calls.filter(
      ([input]) => typeof input === 'string' && input.includes('/v1/platform/summary'),
    );
    expect(summaryCalls).toHaveLength(1);
  });

  it('US-152 — abre o detalhe do estabelecimento por deep link e "Solicitar acesso de suporte" navega para a auditoria de suporte preservando o id', async () => {
    localStorage.setItem(ACCESS_KEY, 'token-admin');
    const tenantId = '11111111-1111-1111-1111-111111111111';
    window.history.pushState({}, '', `/estabelecimentos/${tenantId}`);

    const overviewBody = {
      tenant: {
        id: tenantId,
        name: 'Pizzaria Dona Betinha',
        slug: 'dona-betinha',
        status: 'ACTIVE',
        // US-153 · Ciclo de vida do estabelecimento — statusVersion/availableTransitions passaram
        // a ser obrigatórios no contrato de overview (packages/contracts/src/tenant-overview.ts).
        statusVersion: 5,
        availableTransitions: ['SUSPENDED', 'CANCELLED'],
        plan: 'COMPLETO',
        template: 'PIZZERIA',
        domain: null,
        createdAt: '2026-08-01T12:00:00Z',
        updatedAt: '2026-08-05T12:00:00Z',
      },
      owner: { name: 'Betina Souza', email: 'betina@example.com', inviteStatus: 'ACCEPTED' },
      stores: [],
      installations: [],
      deployment: { completed: 9, total: 9, nextAction: null },
      links: { publicMenu: null, admin: null, health: null },
    };

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes('/v1/platform/summary')) return jsonResponse(SUMMARY_BODY);
        if (url.includes('/overview')) return jsonResponse(overviewBody);
        return jsonResponse({ detail: 'not found' }, 404);
      }),
    );

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Solicitar acesso de suporte/ }));

    expect(await screen.findByRole('heading', { name: 'Solicitar acesso de suporte' })).toBeInTheDocument();
    expect(window.location.pathname).toBe('/auditoria-suporte');
    expect(window.location.search).toBe(`?tenantId=${tenantId}`);
    expect(screen.getByLabelText('Estabelecimento (id)')).toHaveValue(tenantId);
  });

  it('rota desconhecida mostra "recurso inexistente" sem derrubar o shell', async () => {
    localStorage.setItem(ACCESS_KEY, 'token-admin');
    window.history.pushState({}, '', '/rota-desconhecida');
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(SUMMARY_BODY)));

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Página não encontrada' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Estabelecimentos' })).toBeInTheDocument();
  });
});
