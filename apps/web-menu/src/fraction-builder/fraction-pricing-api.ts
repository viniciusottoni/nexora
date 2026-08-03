import {
  previewFractionPricingResponseSchema,
  type PreviewFractionPricingRequest,
  type PreviewFractionPricingResponse,
} from '@nexora/contracts';

/**
 * US-013 (Pizza meio a meio com frações) — cliente HTTP de
 * `POST /v1/catalog/fraction-pricing/preview` (mesmo padrão de
 * `apps/web-admin/src/pricing/pricing-api.ts`, US-014). Isento de `Idempotency-Key` — o
 * back-end (`FractionPricingController.Preview`) está marcado `[IdempotencyExempt]`: é um
 * cálculo puro, sem efeito colateral duplicável.
 *
 * O cliente é público e não anexa o token administrativo do navegador. A API ainda precisa
 * expor uma rota anônima com resolução segura do tenant para que o cardápio público a consuma.
 */
export class FractionPricingApi {
  constructor(
    private readonly baseUrl = '',
    // (...args: Parameters<typeof fetch>) => globalThis.fetch(...args): ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts
    // — `fetch` capturado bruto e chamado depois como `this.fetcher(...)` quebra em navegador real
    // ("Illegal invocation"), mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  async preview(input: PreviewFractionPricingRequest): Promise<PreviewFractionPricingResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/fraction-pricing/preview`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    });
    await requireSuccess(response);
    return previewFractionPricingResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível calcular o preço do meio a meio.');
}
