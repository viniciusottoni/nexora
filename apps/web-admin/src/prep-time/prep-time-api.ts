import {
  prepTimeAnalysisResponseSchema,
  productStationResponseSchema,
  variantPrepTimeResponseSchema,
  type PrepTimeAnalysisResponse,
  type ProductStationResponse,
  type VariantPrepTimeResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-016 (Tempo de preparo e praça por produto) — cliente HTTP dos três endpoints reais desta
 * tarefa: `PATCH /v1/catalog/variants/{id}/prep-time`, `PATCH /v1/catalog/products/{id}/station`
 * e `GET /v1/catalog/variants/{id}/prep-time-analysis` (`Nexora.Api.Cloud.Controllers.ProductPrepTimeController`).
 */

export interface UpdatePrepTimeInput {
  readonly prepMinutes: number;
  readonly warnMinutes: number | null;
  readonly criticalMinutes: number | null;
}

export type { PrepTimeAnalysisResponse, ProductStationResponse, VariantPrepTimeResponse };

export class PrepTimeApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async updatePrepTime(
    variantId: string,
    input: UpdatePrepTimeInput,
  ): Promise<VariantPrepTimeResponse> {
    const response = await this.write(
      `/v1/catalog/variants/${encodeURIComponent(variantId)}/prep-time`,
      {
        method: 'PATCH',
        body: JSON.stringify(input),
      },
    );
    return variantPrepTimeResponseSchema.parse(await response.json());
  }

  async reassignStation(
    productId: string,
    stationId: string | null,
  ): Promise<ProductStationResponse> {
    const response = await this.write(
      `/v1/catalog/products/${encodeURIComponent(productId)}/station`,
      {
        method: 'PATCH',
        body: JSON.stringify({ stationId }),
      },
    );
    return productStationResponseSchema.parse(await response.json());
  }

  async getPrepTimeAnalysis(variantId: string): Promise<PrepTimeAnalysisResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/variants/${encodeURIComponent(variantId)}/prep-time-analysis`,
      { credentials: 'include' },
    );
    await requireSuccess(response);
    return prepTimeAnalysisResponseSchema.parse(await response.json());
  }

  private async write(path: string, init: RequestInit): Promise<Response> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, {
      ...init,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
        ...init.headers,
      },
    });
    await requireSuccess(response);
    return response;
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
