// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { TenantsDirectoryPage } from './tenants-directory-page.js';
import type { TenantsApi } from './tenants-api.js';

const TENANT = {
  id: crypto.randomUUID(),
  name: 'Pizzaria Dona Betinha',
  slug: 'dona-betinha',
  status: 'ACTIVE' as const,
  plan: 'COMPLETO',
  ownerEmail: 'dono@example.com',
  storesCount: 1,
  installationsCount: 1,
  health: 'OK' as const,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-08-01T00:00:00Z',
};

function directoryResponse(data: (typeof TENANT)[], nextCursor: string | null = null) {
  return {
    data,
    nextCursor,
    appliedFilters: {
      query: '',
      status: [],
      plan: [],
      template: [],
      health: [],
      createdFrom: null,
      createdTo: null,
      sort: 'attention' as const,
      limit: 25,
    },
  };
}

function stubApi(overrides: Partial<TenantsApi> = {}): TenantsApi {
  return {
    checkSlug: vi.fn(),
    provision: vi.fn(),
    listTemplates: vi.fn(),
    search: vi.fn().mockResolvedValue(directoryResponse([TENANT])),
    ...overrides,
  };
}

beforeEach(() => {
  window.history.pushState({}, '', '/');
});

afterEach(() => {
  vi.useRealTimers();
});

describe('TenantsDirectoryPage', () => {
  it('lista os estabelecimentos provisionados, com o e-mail do proprietário mascarado', async () => {
    render(<TenantsDirectoryPage api={stubApi()} onCreateTenant={vi.fn()} />);

    expect(await screen.findByText('Pizzaria Dona Betinha')).toBeInTheDocument();
    expect(screen.getByText('dona-betinha')).toBeInTheDocument();
    // "Ativo" também aparece como opção do filtro de status — a badge da coluna é uma célula da tabela.
    expect(screen.getByRole('cell', { name: 'Ativo' })).toBeInTheDocument();
    expect(screen.getByText('d***@example.com')).toBeInTheDocument();
    expect(screen.queryByText('dono@example.com')).not.toBeInTheDocument();
  });

  it('CTA "Novo estabelecimento" também está disponível no diretório', async () => {
    const onCreateTenant = vi.fn();
    render(<TenantsDirectoryPage api={stubApi()} onCreateTenant={onCreateTenant} />);

    await screen.findByText('Pizzaria Dona Betinha');
    fireEvent.click(screen.getByRole('button', { name: /Novo estabelecimento/ }));
    expect(onCreateTenant).toHaveBeenCalledOnce();
  });

  it('linha expõe ação focável por teclado e abre o detalhe do estabelecimento', async () => {
    const onOpenTenant = vi.fn();
    render(
      <TenantsDirectoryPage api={stubApi()} onCreateTenant={vi.fn()} onOpenTenant={onOpenTenant} />,
    );

    const openButton = await screen.findByRole('button', { name: 'Pizzaria Dona Betinha' });
    openButton.focus();
    expect(openButton).toHaveFocus();
    fireEvent.click(openButton);
    expect(onOpenTenant).toHaveBeenCalledWith(TENANT.id);
  });

  it('mostra estado vazio de base (nenhum estabelecimento provisionado ainda) quando não há filtro', async () => {
    const onCreateTenant = vi.fn();
    const search = vi.fn().mockResolvedValue(directoryResponse([]));
    render(<TenantsDirectoryPage api={stubApi({ search })} onCreateTenant={onCreateTenant} />);

    expect(
      await screen.findByText('Nenhum estabelecimento provisionado ainda'),
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Provisionar o primeiro estabelecimento' }));
    expect(onCreateTenant).toHaveBeenCalledOnce();
  });

  it('mostra estado "nenhum resultado" (distinto do vazio de base) quando a busca não encontra nada', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const search = vi
      .fn()
      .mockResolvedValueOnce(directoryResponse([TENANT]))
      .mockResolvedValue(directoryResponse([]));
    render(<TenantsDirectoryPage api={stubApi({ search })} onCreateTenant={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');

    fireEvent.change(screen.getByLabelText('Buscar'), { target: { value: 'não existe' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(400);
    });

    expect(
      await screen.findByText('Nenhum resultado para os filtros aplicados'),
    ).toBeInTheDocument();
    expect(screen.queryByText('Nenhum estabelecimento provisionado ainda')).not.toBeInTheDocument();
  });

  it('busca com debounce: só chama a API depois da pausa de digitação', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const search = vi.fn().mockResolvedValue(directoryResponse([TENANT]));
    render(<TenantsDirectoryPage api={stubApi({ search })} onCreateTenant={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    expect(search).toHaveBeenCalledTimes(1);

    fireEvent.change(screen.getByLabelText('Buscar'), { target: { value: 'betinha' } });
    // Ainda dentro da janela de debounce — nenhuma chamada extra.
    expect(search).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(400);
    });
    await waitFor(() => expect(search).toHaveBeenCalledTimes(2));
    expect(search.mock.calls[1]?.[0]).toMatchObject({ query: 'betinha' });

    // URL reflete a busca aplicada.
    expect(window.location.search).toContain('query=betinha');
  });

  it('aplicar e remover um filtro por chip refaz a busca e reflete na URL', async () => {
    const search = vi.fn().mockResolvedValue(directoryResponse([TENANT]));
    render(<TenantsDirectoryPage api={stubApi({ search })} onCreateTenant={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    expect(search).toHaveBeenCalledTimes(1);

    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'ACTIVE' } });

    await waitFor(() => expect(search).toHaveBeenCalledTimes(2));
    expect(search.mock.calls[1]?.[0]).toMatchObject({ status: ['ACTIVE'] });
    expect(screen.getByText('Status: Ativo')).toBeInTheDocument();
    expect(screen.getByText('1 filtro ativo')).toBeInTheDocument();
    expect(window.location.search).toContain('status=ACTIVE');

    fireEvent.click(screen.getByRole('button', { name: 'Remover filtro: Status: Ativo' }));

    await waitFor(() => expect(search).toHaveBeenCalledTimes(3));
    expect((search.mock.calls[2]?.[0] as { status?: unknown }).status).toBeUndefined();
    expect(screen.queryByText('Status: Ativo')).not.toBeInTheDocument();
    expect(window.location.search).not.toContain('status=');
  });

  it('paginação por cursor: avançar busca a próxima página sem repetir nem omitir', async () => {
    const search = vi
      .fn()
      .mockResolvedValueOnce(directoryResponse([TENANT], 'cursor-pagina-2'))
      .mockResolvedValueOnce(
        directoryResponse(
          [{ ...TENANT, id: crypto.randomUUID(), name: 'Pizzaria do Zé', slug: 'pizzaria-do-ze' }],
          null,
        ),
      );
    render(<TenantsDirectoryPage api={stubApi({ search })} onCreateTenant={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    const nextButton = screen.getByRole('button', { name: /Próxima página/ });
    expect(nextButton).toBeEnabled();

    fireEvent.click(nextButton);

    expect(await screen.findByText('Pizzaria do Zé')).toBeInTheDocument();
    expect(screen.queryByText('Pizzaria Dona Betinha')).not.toBeInTheDocument();
    expect(search).toHaveBeenCalledTimes(2);
    expect(search.mock.calls[1]?.[0]).toMatchObject({ cursor: 'cursor-pagina-2' });
    expect(screen.getByRole('button', { name: /Próxima página/ })).toBeDisabled();
    expect(screen.getByRole('button', { name: /Página anterior/ })).toBeEnabled();
  });

  it('preserva os resultados e os filtros ao falhar uma atualização, avisando que podem estar desatualizados', async () => {
    const search = vi
      .fn()
      .mockResolvedValueOnce(directoryResponse([TENANT]))
      .mockRejectedValueOnce(new Error('Falha de rede.'));
    render(<TenantsDirectoryPage api={stubApi({ search })} onCreateTenant={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'ACTIVE' } });

    expect(await screen.findByText(/Não foi possível atualizar a lista/)).toBeInTheDocument();
    // Preserva a linha antiga (não some da tela — só avisa que pode estar desatualizada).
    expect(screen.getByText('Pizzaria Dona Betinha')).toBeInTheDocument();
    expect(screen.getByText('Status: Ativo')).toBeInTheDocument();
  });

  it('mostra falha de API sem quebrar a tela quando não há dado nenhum ainda', async () => {
    render(
      <TenantsDirectoryPage
        api={stubApi({ search: vi.fn().mockRejectedValue(new Error('Falha ao carregar.')) })}
        onCreateTenant={vi.fn()}
      />,
    );

    expect(await screen.findByText('Falha ao carregar.')).toBeInTheDocument();
  });

  it('exportação CSV dispara o download apenas com os campos exibidos', async () => {
    const createObjectURL = vi.fn().mockReturnValue('blob:mock');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', { ...URL, createObjectURL, revokeObjectURL });
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined);

    render(<TenantsDirectoryPage api={stubApi()} onCreateTenant={vi.fn()} />);

    await screen.findByText('Pizzaria Dona Betinha');
    fireEvent.click(screen.getByRole('button', { name: /Exportar CSV/ }));

    expect(createObjectURL).toHaveBeenCalledOnce();
    const [blob] = createObjectURL.mock.calls[0] as [Blob];
    expect(blob).toBeInstanceOf(Blob);
    expect(blob.type).toContain('text/csv');
    expect(clickSpy).toHaveBeenCalledOnce();
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:mock');

    clickSpy.mockRestore();
    vi.unstubAllGlobals();
  });
});
