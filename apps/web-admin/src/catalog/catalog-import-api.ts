import {
  catalogImportCommitResponseSchema,
  catalogImportValidateResponseSchema,
  type CatalogImportCommitResponse,
  type CatalogImportValidateResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/** Nome do arquivo gerado por `GET /v1/catalog/import/template` (mesmo valor de `GetCatalogImportTemplateQueryHandler`, back-end). */
export const CATALOG_IMPORT_TEMPLATE_FILENAME = 'modelo-importacao-cardapio.xlsx';

/**
 * US-144 (Importação de cardápio por planilha) — baixa o modelo, valida (pré-visualização, nunca
 * grava nada) e confirma a importação. `commit` é deliberadamente diferente de `write` das outras
 * APIs de catálogo: 201 (sucesso) e 422 (linhas inválidas) são os dois desfechos ESPERADOS do corpo
 * `CatalogImportCommitResponse` — só status fora desses dois vira exceção (ver
 * `CatalogImportController`, back-end: o handler nunca devolve `Result.Failure` por linha
 * inválida).
 */
export class CatalogImportApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async downloadTemplate(): Promise<Blob> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/import/template`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return response.blob();
  }

  async validate(file: File): Promise<CatalogImportValidateResponse> {
    const response = await this.postFile('/v1/catalog/import/validate', file);
    await requireSuccess(response);
    return catalogImportValidateResponseSchema.parse(await response.json());
  }

  async commit(file: File): Promise<CatalogImportCommitResponse> {
    const response = await this.postFile('/v1/catalog/import', file);
    if (response.status !== 201 && response.status !== 422) {
      await requireSuccess(response);
    }
    return catalogImportCommitResponseSchema.parse(await response.json());
  }

  private async postFile(path: string, file: File): Promise<Response> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    // Sem Content-Type manual: o navegador define multipart/form-data com o boundary correto ao
    // ver um body FormData — sobrescrever aqui quebraria o parsing do lado do servidor.
    return this.fetcher(`${this.baseUrl}${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Idempotency-Key': crypto.randomUUID() },
      body: formData,
    });
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
