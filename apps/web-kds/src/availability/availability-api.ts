import {
  productAvailabilityChangedEventSchema,
  productAvailabilitySchema,
  unavailableProductsResponseSchema,
  type ProductAvailabilityChangedEvent,
  type ProductAvailabilityDto,
  type UnavailableProductsResponse,
} from '@nexora/contracts';

export type {
  ProductAvailabilityChangedEvent,
  ProductAvailabilityDto,
  UnavailableProductsResponse,
};

/**
 * US-015 (Marcar produto indisponível com propagação imediata) — cliente HTTP + realtime do KDS.
 */

/**
 * Cliente HTTP de `POST /v1/kds/products/:id/unavailable`, `POST /v1/kds/products/:id/available` e
 * `GET /v1/kds/products/unavailable` — mesmo padrão de `ProductsApi`/`DevicesApi` (fetcher
 * injetável para teste, `Idempotency-Key` nova em toda escrita — ADR-020).
 */
export class AvailabilityApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = fetch,
    private readonly accessToken?: string,
  ) {}

  async markUnavailable(
    productId: string,
    reason: string,
    autoRestoreNextDay = true,
  ): Promise<ProductAvailabilityDto> {
    return this.write(`/v1/kds/products/${encodeURIComponent(productId)}/unavailable`, {
      method: 'POST',
      body: JSON.stringify({ reason, autoRestoreNextDay }),
    });
  }

  async markAvailable(productId: string): Promise<ProductAvailabilityDto> {
    return this.write(`/v1/kds/products/${encodeURIComponent(productId)}/available`, {
      method: 'POST',
      body: '{}',
    });
  }

  async listUnavailable(): Promise<UnavailableProductsResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/kds/products/unavailable`, {
      credentials: 'include',
      headers: this.authorizationHeaders(),
    });
    await requireSuccess(response);
    return unavailableProductsResponseSchema.parse(await response.json());
  }

  private async write(path: string, init: RequestInit): Promise<ProductAvailabilityDto> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, {
      ...init,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
        ...this.authorizationHeaders(),
        ...init.headers,
      },
    });
    await requireSuccess(response);
    return productAvailabilitySchema.parse(await response.json());
  }

  private authorizationHeaders(): HeadersInit {
    return this.accessToken ? { Authorization: `Bearer ${this.accessToken}` } : {};
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}

// ---------------------------------------------------------------------------
// Realtime: cliente minimo do protocolo "JSON Hub Protocol" do SignalR sobre WebSocket nativo do
// navegador (US-015 - "WebSocket nativo do browser ou uma lib leve ja presente no monorepo"; nao
// ha @microsoft/signalr no monorepo, e adiciona-lo e uma dependencia nova so para isto - em vez
// disso, implementamos o subconjunto do protocolo que precisamos: handshake + frames de invocacao,
// ambos separados pelo caractere ASCII 0x1E (record separator, RS). Funciona porque o servidor
// aceita a conexao WebSocket diretamente em /hubs/catalog-availability (equivalente a
// `skipNegotiation: true` do cliente oficial) - ver Nexora.Api.Edge/Nexora.Api.Cloud Program.cs,
// que so chamam `app.MapHub<CatalogAvailabilityHub>`, sem exigir o passo HTTP /negotiate.
// ---------------------------------------------------------------------------

const RECORD_SEPARATOR = String.fromCharCode(0x1e);

/** Frame de handshake do protocolo JSON do SignalR - primeira mensagem enviada apos abrir a conexao. */
export function buildHandshakeFrame(): string {
  return `${JSON.stringify({ protocol: 'json', version: 1 })}${RECORD_SEPARATOR}`;
}

/**
 * Separa um buffer (que pode conter varias mensagens concatenadas, ou uma mensagem incompleta) em
 * mensagens completas (parseadas) + o restante ainda incompleto (aguarda mais dados do socket).
 * Funcao pura - testavel sem WebSocket nenhum.
 */
export function splitHubFrames(buffer: string): {
  readonly messages: readonly unknown[];
  readonly remainder: string;
} {
  const parts = buffer.split(RECORD_SEPARATOR);
  const remainder = parts.pop() ?? '';
  const messages = parts
    .filter((part) => part.length > 0)
    .map((part) => JSON.parse(part) as unknown);
  return { messages, remainder };
}

interface HubInvocationMessage {
  readonly type: 1;
  readonly target: string;
  readonly arguments: readonly unknown[];
}

function isInvocationMessage(value: unknown): value is HubInvocationMessage {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return (
    candidate.type === 1 &&
    typeof candidate.target === 'string' &&
    Array.isArray(candidate.arguments)
  );
}

function isAvailabilityChangedEvent(value: unknown): value is ProductAvailabilityChangedEvent {
  return productAvailabilityChangedEventSchema.safeParse(value).success;
}

/** Superficie minima de `WebSocket` de que este modulo precisa - permite injetar um duplo de teste. */
export interface WebSocketLike {
  onopen: (() => void) | null;
  onmessage: ((event: { readonly data: string }) => void) | null;
  onclose: (() => void) | null;
  onerror: (() => void) | null;
  send(data: string): void;
  close(): void;
}

export interface AvailabilitySubscriptionOptions {
  readonly baseUrl?: string;
  /** JWT do usuario/dispositivo autenticado - vai como `?access_token=` (o navegador nao anexa header em WebSocket). */
  readonly accessToken?: string;
  /** Intervalo do fallback de polling quando o WebSocket cai (US-015 §9: "no maximo 5 segundos"). */
  readonly pollIntervalMs?: number;
  readonly webSocketFactory?: (url: string) => WebSocketLike;
  readonly setIntervalFn?: typeof setInterval;
  readonly clearIntervalFn?: typeof clearInterval;
  readonly api?: AvailabilityApi;
}

export interface AvailabilitySubscription {
  close(): void;
}

function buildHubUrl(baseUrl: string, accessToken?: string): string {
  const hasOrigin = /^https?:\/\//i.test(baseUrl);
  const parsed = hasOrigin ? new URL(baseUrl) : undefined;
  const origin =
    parsed?.origin ?? (typeof location !== 'undefined' ? location.origin : 'http://localhost');
  const path = `${parsed?.pathname ?? baseUrl}/hubs/catalog-availability`;
  const query = accessToken ? `?access_token=${encodeURIComponent(accessToken)}` : '';
  return `${origin.replace(/^http/i, 'ws')}${path}${query}`;
}

function defaultWebSocketFactory(url: string): WebSocketLike {
  return new WebSocket(url) as unknown as WebSocketLike;
}

/**
 * Assina as mudancas de disponibilidade em tempo real (US-015 §4: "em ate 2 segundos"). Tenta o
 * WebSocket do hub primeiro; se a conexao cair ou nao puder ser aberta, cai automaticamente para
 * polling a cada `pollIntervalMs` (padrao 5000 - US-015 §9/ADR-011: "se o WebSocket cair, o
 * dispositivo faz polling a cada 5 segundos"). Nao tenta reconectar o WebSocket depois de cair -
 * [PROXIMO PASSO] reconexao com backoff fica para quando este modulo tiver mais tempo de forno;
 * o polling ja garante a entrega da mudanca, so nao e "em tempo real".
 */
export function subscribeToAvailability(
  onChange: (event: ProductAvailabilityChangedEvent) => void,
  options: AvailabilitySubscriptionOptions = {},
): AvailabilitySubscription {
  const pollIntervalMs = options.pollIntervalMs ?? 5000;
  const api = options.api ?? new AvailabilityApi(options.baseUrl ?? '', fetch, options.accessToken);
  const setIntervalFn = options.setIntervalFn ?? setInterval;
  const clearIntervalFn = options.clearIntervalFn ?? clearInterval;
  const webSocketFactory = options.webSocketFactory ?? defaultWebSocketFactory;

  let closed = false;
  let pollTimer: ReturnType<typeof setInterval> | undefined;
  let socket: WebSocketLike | undefined;
  let buffer = '';
  let polledUnavailableIds = new Set<string>();

  function toChangeEvent(item: ProductAvailabilityDto): ProductAvailabilityChangedEvent {
    return {
      type: item.isAvailable ? 'product.available' : 'product.unavailable',
      data: {
        productId: item.productId,
        ...(item.unavailableReason ? { reason: item.unavailableReason } : {}),
        ...(item.unavailableSince ? { unavailableSince: item.unavailableSince } : {}),
      },
    };
  }

  function startPolling(): void {
    if (pollTimer || closed) return;
    pollTimer = setIntervalFn(() => {
      api
        .listUnavailable()
        .then((result) => {
          const nextUnavailableIds = new Set(result.items.map((item) => item.productId));
          for (const item of result.items) onChange(toChangeEvent(item));
          for (const productId of polledUnavailableIds) {
            if (!nextUnavailableIds.has(productId)) {
              onChange({ type: 'product.available', data: { productId } });
            }
          }
          polledUnavailableIds = nextUnavailableIds;
        })
        .catch(() => {
          // Fallback de polling ja assume falhas transitorias - so tenta de novo no proximo tick.
        });
    }, pollIntervalMs);
  }

  function handleMessage(event: { readonly data: string }): void {
    buffer += event.data;
    const { messages, remainder } = splitHubFrames(buffer);
    buffer = remainder;

    for (const message of messages) {
      if (!isInvocationMessage(message) || message.target !== 'productAvailabilityChanged')
        continue;
      const payload = message.arguments[0];
      if (isAvailabilityChangedEvent(payload)) onChange(payload);
    }
  }

  function connect(): void {
    if (closed) return;

    try {
      socket = webSocketFactory(buildHubUrl(options.baseUrl ?? '', options.accessToken));
    } catch {
      startPolling();
      return;
    }

    socket.onopen = () => socket?.send(buildHandshakeFrame());
    socket.onmessage = handleMessage;
    socket.onclose = () => {
      socket = undefined;
      if (!closed) startPolling();
    };
    socket.onerror = () => socket?.close();
  }

  connect();

  return {
    close() {
      closed = true;
      if (pollTimer) clearIntervalFn(pollTimer);
      socket?.close();
    },
  };
}
