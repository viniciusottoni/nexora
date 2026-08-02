import { priceSchema, type PriceDto, type SetVariantPriceRequest } from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/**
 * US-011 (Variações de produto com preço próprio) — só o preço "base" de uma variante em um
 * único canal (padrão `DineIn` quando `channel` não é enviado). A tabela de preço por canal
 * completa (todos os canais lado a lado, ajuste em massa, auditoria dedicada) é escopo da
 * US-014 e não tem cliente próprio ainda — quando essa história for implementada, é aqui que o
 * método de "listar histórico"/"ajuste em massa" deve entrar.
 */
export class PricesApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  /** Fecha o preço vigente do canal e cria uma nova linha — nunca edita um preço existente (histórico preservado, US-011 §4). */
  async setVariantPrice(variantId: string, input: SetVariantPriceRequest): Promise<PriceDto> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/variants/${encodeURIComponent(variantId)}/prices`,
      {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
        },
        body: JSON.stringify(input),
      },
    );
    await requireSuccess(response);
    return priceSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
