// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CatalogImportApi } from './catalog-import-api.js';
import { CatalogImportPage } from './catalog-import-page.js';

// jsdom não implementa URL.createObjectURL/revokeObjectURL (usados por "Baixar modelo") — stub
// mínimo só para o download não lançar "not implemented" durante o teste.
vi.stubGlobal('URL', { ...URL, createObjectURL: vi.fn(() => 'blob:mock'), revokeObjectURL: vi.fn() });

function makeFile(): File {
  return new File(['conteudo'], 'cardapio.xlsx', {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
}

function selectFile(): void {
  const input = screen.getByLabelText('Selecionar arquivo .xlsx', { exact: false }) as HTMLInputElement;
  fireEvent.change(input, { target: { files: [makeFile()] } });
}

afterEach(() => {
  cleanup();
});

function renderPage(api: CatalogImportApi): void {
  cleanup();
  render(<CatalogImportPage api={api} />);
}

describe('CatalogImportPage', () => {
  it('mostra os erros por linha quando a planilha é inválida', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(
          JSON.stringify({
            valid: false,
            errors: [{ row: 12, column: 'preco', message: 'Valor inválido' }],
            preview: {
              toCreate: { categories: 0, products: 0, variants: 0 },
              toUpdate: { categories: 0, products: 0, variants: 0 },
            },
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        ),
    );
    const api = new CatalogImportApi('/api', fetcher);
    renderPage(api);

    selectFile();
    fireEvent.click(screen.getByRole('button', { name: 'Validar planilha' }));

    await waitFor(() => expect(screen.getByText('Valor inválido')).toBeInTheDocument());
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('preco')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Confirmar importação' })).not.toBeInTheDocument();
  });

  it('mostra a pré-visualização e confirma a importação com sucesso', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/v1/catalog/import/validate')) {
        return new Response(
          JSON.stringify({
            valid: true,
            errors: [],
            preview: {
              toCreate: { categories: 1, products: 1, variants: 2 },
              toUpdate: { categories: 1, products: 0, variants: 0 },
            },
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }
      return new Response(
        JSON.stringify({
          valid: true,
          errors: [],
          created: { categories: 1, products: 1, variants: 2 },
          updated: { categories: 1, products: 0, variants: 0 },
          skipped: 0,
        }),
        { status: 201, headers: { 'Content-Type': 'application/json' } },
      );
    });
    const api = new CatalogImportApi('/api', fetcher);
    renderPage(api);

    selectFile();
    fireEvent.click(screen.getByRole('button', { name: 'Validar planilha' }));

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Confirmar importação' })).toBeInTheDocument(),
    );
    expect(screen.getByText('Categorias atualizadas')).toBeInTheDocument();

    const confirmButton = screen.getByText('Confirmar importação');
    fireEvent.click(confirmButton);

    await waitFor(() => expect(screen.getByText('Importação concluída')).toBeInTheDocument());
    expect(screen.getByText(/1 categoria\(s\), 1 produto\(s\)/)).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent(
      /1 categoria\(s\),\s*0 produto\(s\) e 0 variação\(ões\) atualizados\./,
    );
  });

  it('baixa o modelo de planilha ao clicar em "Baixar modelo"', async () => {
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(new Blob(['bytes']), {
          status: 200,
          headers: { 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
        }),
    );
    const api = new CatalogImportApi('/api', fetcher);
    renderPage(api);

    const downloadButton = screen.getByRole('button', { name: /Baixar modelo/ });
    fireEvent.click(downloadButton);

    await waitFor(() => expect(fetcher).toHaveBeenCalledWith(
      '/api/v1/catalog/import/template',
      expect.anything(),
    ));
    clickSpy.mockRestore();
  });
});
