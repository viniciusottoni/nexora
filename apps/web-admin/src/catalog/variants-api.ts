import {
  variantListResponseSchema,
  variantSchema,
  type CreateVariantRequest,
  type UpdateVariantRequest,
  type VariantDto,
  type VariantListResponse,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-011 (Variações de produto com preço próprio) — CRUD de variantes. Preço não é gerenciado
 * aqui: `POST .../variants` já recebe o preço base na criação (ver `CreateVariantRequest`), e
 * qualquer alteração de preço posterior passa por `PricesApi.setVariantPrice`
 * (`prices-api.ts`) — mantidos como dois clientes separados (em vez de um único
 * `VariantsAndPricesApi`) porque são dois sub-recursos com ciclo de vida distinto: uma variante é
 * editada por PATCH, um preço nunca é (é sempre uma linha nova, histórico preservado).
 */
export class VariantsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async listForProduct(productId: string): Promise<VariantListResponse> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/products/${encodeURIComponent(productId)}/variants`,
      {
        credentials: 'include',
      },
    );
    await requireSuccess(response);
    return variantListResponseSchema.parse(await response.json());
  }

  async create(productId: string, input: CreateVariantRequest): Promise<VariantDto> {
    return this.write(`/v1/catalog/products/${encodeURIComponent(productId)}/variants`, {
      method: 'POST',
      body: JSON.stringify(input),
    });
  }

  async update(id: string, input: UpdateVariantRequest): Promise<VariantDto> {
    return this.write(`/v1/catalog/variants/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  async activate(id: string): Promise<VariantDto> {
    return this.write(`/v1/catalog/variants/${encodeURIComponent(id)}/activate`, {
      method: 'POST',
      body: '{}',
    });
  }

  /** Nunca exclui fisicamente — não existe endpoint de exclusão de variante (US-011 §3.1). */
  async deactivate(id: string): Promise<VariantDto> {
    return this.write(`/v1/catalog/variants/${encodeURIComponent(id)}/deactivate`, {
      method: 'POST',
      body: '{}',
    });
  }

  async markAsDefault(id: string): Promise<VariantDto> {
    return this.write(`/v1/catalog/variants/${encodeURIComponent(id)}/mark-default`, {
      method: 'POST',
      body: '{}',
    });
  }

  private async write(path: string, init: RequestInit): Promise<VariantDto> {
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
    return variantSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
