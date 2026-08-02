import {
  publicMenuResponseSchema,
  publicTableAccessResponseSchema,
  type PublicMenuResponse,
  type PublicTableAccessResponse,
} from '@nexora/contracts';

/**
 * Erro de negócio de acesso público — carrega o código estável do ProblemDetails (ADR-021), para
 * a tela decidir a mensagem certa (ex.: `INVALID_TABLE_TOKEN` -&gt; "chame o garçom") sem
 * depender do texto de `detail`.
 */
export class PublicTableApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
  ) {
    super(message);
    this.name = 'PublicTableApiError';
  }
}

/**
 * Cliente de `GET /v1/public/table/{qrToken}` e `GET /v1/public/menu?channel=DINE_IN` no EDGE
 * (US-021) — os dois endpoints são `[AllowAnonymous]`, servidos pelo Nginx do edge na LAN da
 * loja (US-021 §9), então nenhuma chamada aqui carrega token de staff nem cookie de sessão
 * administrativa.
 */
export class PublicTableApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = fetch,
  ) {}

  /**
   * Cenários Gherkin "Primeira leitura da mesa"/"Sessão de mesa já aberta"/"Token inválido ou
   * rotacionado" (US-021 §4). Nunca lança para token inválido de um jeito que vaze detalhe — o
   * chamador lê `error.code === 'INVALID_TABLE_TOKEN'` e mostra a mensagem amigável (US-021 §10).
   */
  async accessTable(qrToken: string): Promise<PublicTableAccessResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/public/table/${encodeURIComponent(qrToken)}`, {
      headers: { Accept: 'application/json' },
    });
    await requireSuccess(response);
    return publicTableAccessResponseSchema.parse(await response.json());
  }

  /** Cardápio do canal de mesa — carregado logo depois de `accessTable` resolver a mesa/sessão. */
  async getMenu(): Promise<PublicMenuResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/public/menu?channel=DINE_IN`, {
      headers: { Accept: 'application/json' },
    });
    await requireSuccess(response);
    return publicMenuResponseSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string; code?: string } | null;
  throw new PublicTableApiError(problem?.detail ?? 'Não foi possível concluir a operação.', problem?.code);
}
