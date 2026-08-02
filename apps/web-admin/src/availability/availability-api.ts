import { authenticatedFetch } from '@nexora/ui';
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
 * US-015 (Marcar produto indisponível com propagação imediata) — cliente HTTP + realtime da nuvem
 * (painel de gestão).
 *
 */

/**
 * Cliente HTTP de `POST /v1/catalog/products/:id/availability` e
 * `GET /v1/catalog/products/unavailable` — mesmo padrão de `ProductsApi`/`DevicesApi` (fetcher
 * injetável para teste, `Idempotency-Key` nova em toda escrita — ADR-020).
 */
export class AvailabilityApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  /** Retorna manualmente um produto à disponibilidade (US-015 §3.1, "manual ou automático"). */
  async markAvailable(productId: string): Promise<ProductAvailabilityDto> {
    return this.write(`/v1/catalog/products/${encodeURIComponent(productId)}/availability`, {
      method: 'POST',
      body: JSON.stringify({ isAvailable: true }),
    });
  }

  async markUnavailable(
    productId: string,
    reason: string,
    autoRestoreNextDay = true,
  ): Promise<ProductAvailabilityDto> {
    return this.write(`/v1/catalog/products/${encodeURIComponent(productId)}/availability`, {
      method: 'POST',
      body: JSON.stringify({ isAvailable: false, reason, autoRestoreNextDay }),
    });
  }

  /** "Lista de itens indisponíveis sempre visível ao gestor" (US-015 §10). */
  async listUnavailable(): Promise<UnavailableProductsResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/products/unavailable`, {
      credentials: 'include',
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
        ...init.headers,
      },
    });
    await requireSuccess(response);
    return productAvailabilitySchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}

// ---------------------------------------------------------------------------
// Realtime — mesmo cliente mínimo de "JSON Hub Protocol" sobre WebSocket nativo descrito em
// apps/web-kds/src/availability/availability-api.ts (duplicado aqui deliberadamente: os dois apps
// não compartilham hoje um módulo comum para isto, e criar um pacote novo só para a US-015 é fora
// do escopo desta tarefa — [PRÓXIMO PASSO] mover para @nexora/ui ou um novo @nexora/realtime
// quando um terceiro consumidor precisar do mesmo protocolo).
// ---------------------------------------------------------------------------

const RECORD_SEPARATOR = String.fromCharCode(0x1e);

export function buildHandshakeFrame(): string {
  return `${JSON.stringify({ protocol: 'json', version: 1 })}${RECORD_SEPARATOR}`;
}

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
  readonly accessToken?: string;
  /** Fallback de polling quando o WebSocket cai (US-015 §9/ADR-011: no máximo 5 segundos). */
  readonly pollIntervalMs?: number;
  readonly webSocketFactory?: (url: string) => WebSocketLike;
  readonly setIntervalFn?: typeof setInterval;
  readonly clearIntervalFn?: typeof clearInterval;
  readonly api?: AvailabilityApi;
}

export interface AvailabilitySubscription {
  close(): void;
}

/**
 * Chave de armazenamento da sessão da nuvem — mesma constante privada de
 * `packages/ui/src/auth/cloud-auth.tsx` (`ACCESS_KEY`), não exportada por aquele módulo. Duplicada
 * aqui só para montar a URL do WebSocket (`?access_token=`, o navegador não anexa header
 * `Authorization` em conexões WebSocket) — [PRÓXIMO PASSO] `@nexora/ui` deveria exportar um
 * `getCloudAccessToken()` para este acoplamento deixar de ser um literal duplicado.
 */
const CLOUD_ACCESS_TOKEN_STORAGE_KEY = 'food-operations.cloud.access';

function resolveAccessToken(explicit?: string): string | undefined {
  if (explicit) return explicit;
  if (typeof localStorage === 'undefined') return undefined;
  return localStorage.getItem(CLOUD_ACCESS_TOKEN_STORAGE_KEY) ?? undefined;
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
 * Assina as mudanças de disponibilidade em tempo real para manter a lista de itens indisponíveis
 * sempre atualizada (US-015 §4/§10). Mesma estratégia de `apps/web-kds`: WebSocket primeiro,
 * polling a cada 5s como fallback se a conexão cair.
 */
export function subscribeToAvailability(
  onChange: (event: ProductAvailabilityChangedEvent) => void,
  options: AvailabilitySubscriptionOptions = {},
): AvailabilitySubscription {
  const pollIntervalMs = options.pollIntervalMs ?? 5000;
  const api = options.api ?? new AvailabilityApi(options.baseUrl ?? '');
  const setIntervalFn = options.setIntervalFn ?? setInterval;
  const clearIntervalFn = options.clearIntervalFn ?? clearInterval;
  const webSocketFactory = options.webSocketFactory ?? defaultWebSocketFactory;
  const accessToken = resolveAccessToken(options.accessToken);

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
          // Fallback de polling já assume falhas transitórias — só tenta de novo no próximo tick.
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
      socket = webSocketFactory(buildHubUrl(options.baseUrl ?? '', accessToken));
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
