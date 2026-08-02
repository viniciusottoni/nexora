import {
  bulkAdjustPricesResponseSchema,
  variantPriceTableResponseSchema,
  type BulkAdjustPricesRequest,
  type BulkAdjustPricesResponse,
  type SetVariantChannelPriceRequest,
  type VariantPriceTableResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-014 (Preço por canal de venda) — cliente HTTP dos três endpoints do módulo:
 * `GET/PUT /v1/catalog/variants/:id/prices` (tabela por canal) e
 * `POST /v1/catalog/prices/bulk-adjust` (reajuste em massa por categoria).
 *
 * NOTA DE INTEGRAÇÃO: importa `BulkAdjustPricesRequest`/`BulkAdjustPricesResponse`/
 * `SetVariantChannelPriceRequest`/`VariantPriceTableResponse`/`bulkAdjustPricesResponseSchema`/
 * `variantPriceTableResponseSchema` de `@nexora/contracts` — isso só resolve depois que
 * `packages/contracts/src/index.ts` ganhar `export * from './catalog-price-adjustments.js';`
 * (arquivo proibido de editar nesta tarefa, ver relatório). Até lá, `pnpm typecheck` acusa esses
 * símbolos como inexistentes no pacote — falha esperada, documentada no relatório da tarefa.
 */
export class PricingApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  /** Tabela de preço por canal de uma variante — os quatro canais, com herança do preço base já resolvida (US-014 §10). */
  async getPriceTable(variantId: string): Promise<VariantPriceTableResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/variants/${encodeURIComponent(variantId)}/prices`,
      {
        credentials: 'include',
      },
    );
    await requireSuccess(response);
    return variantPriceTableResponseSchema.parse(await response.json());
  }

  /** Define o preço vigente de um ou mais canais na mesma chamada — fecha o vigente e cria uma nova linha por canal alterado (histórico preservado). */
  async setPriceTable(
    variantId: string,
    input: SetVariantChannelPriceRequest,
  ): Promise<VariantPriceTableResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/variants/${encodeURIComponent(variantId)}/prices`,
      {
        method: 'PUT',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
        },
        body: JSON.stringify(input),
      },
    );
    await requireSuccess(response);
    return variantPriceTableResponseSchema.parse(await response.json());
  }

  /** Reajuste percentual em massa — aplica sobre o preço efetivo de um canal para todas as variações ativas de uma categoria, em uma única transação. */
  async bulkAdjust(input: BulkAdjustPricesRequest): Promise<BulkAdjustPricesResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/prices/bulk-adjust`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
      },
      body: JSON.stringify(input),
    });
    await requireSuccess(response);
    return bulkAdjustPricesResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
