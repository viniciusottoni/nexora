import { describe, expect, it, vi } from 'vitest';
import { CatalogImportApi } from './catalog-import-api.js';

const validateResponse = {
  valid: false,
  errors: [{ row: 12, column: 'preco', message: 'Valor inválido' }],
  preview: {
    toCreate: { categories: 0, products: 0, variants: 0 },
    toUpdate: { categories: 0, products: 0, variants: 0 },
  },
};

const commitSuccessResponse = {
  valid: true,
  errors: [],
  created: { categories: 6, products: 57, variants: 132 },
  updated: { categories: 0, products: 0, variants: 0 },
  skipped: 0,
};

const commitInvalidResponse = {
  valid: false,
  errors: [{ row: 3, column: 'produto', message: 'Produto é obrigatório.' }],
  created: { categories: 0, products: 0, variants: 0 },
  updated: { categories: 0, products: 0, variants: 0 },
  skipped: 0,
};

function makeFile(): File {
  return new File(['conteudo'], 'cardapio.xlsx', {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
}

describe('CatalogImportApi', () => {
  it('baixa o modelo como blob', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(new Blob(['bytes']), {
          status: 200,
          headers: { 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
        }),
    );
    const api = new CatalogImportApi('/api', fetcher);

    const blob = await api.downloadTemplate();

    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/catalog/import/template');
    expect(blob).toBeInstanceOf(Blob);
  });

  it('valida a planilha via multipart, sempre com Idempotency-Key', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(validateResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new CatalogImportApi('/api', fetcher);

    const result = await api.validate(makeFile());

    expect(result.valid).toBe(false);
    expect(result.errors).toHaveLength(1);
    const [url, init] = fetcher.mock.calls[0]!;
    expect(url).toBe('/api/v1/catalog/import/validate');
    expect(init?.body).toBeInstanceOf(FormData);
    expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
    // Content-Type não deve ser fixado manualmente — o navegador define o boundary do multipart.
    expect(new Headers(init?.headers).get('Content-Type')).toBeNull();
  });

  it('commit trata 201 como sucesso', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(commitSuccessResponse), {
          status: 201,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new CatalogImportApi('/api', fetcher);

    const result = await api.commit(makeFile());

    expect(result.valid).toBe(true);
    expect(result.created.products).toBe(57);
  });

  it('commit trata 422 como um resultado válido (linhas inválidas), não como exceção', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(commitInvalidResponse), {
          status: 422,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new CatalogImportApi('/api', fetcher);

    const result = await api.commit(makeFile());

    expect(result.valid).toBe(false);
    expect(result.errors[0]?.message).toContain('obrigatório');
  });

  it('commit lança exceção para status inesperado (ex.: 400 de arquivo inválido)', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Envie um arquivo .xlsx de até 10 MB.' }), {
          status: 400,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
    );
    const api = new CatalogImportApi('/api', fetcher);

    await expect(api.commit(makeFile())).rejects.toThrow('Envie um arquivo .xlsx de até 10 MB.');
  });
});
