import {
  billResponseSchema,
  sessionConsumptionResponseSchema,
  tableConsumptionEventSchema,
  type BillResponse,
  type SessionConsumptionResponse,
  type TableConsumptionEvent,
} from '@nexora/contracts';
import * as signalR from '@microsoft/signalr';

/** Erro de negócio do consumo/repetição — carrega o código estável do ProblemDetails (ADR-021). */
export class ConsumptionApiError extends Error {
  constructor(
    message: string,
    readonly code?: string,
  ) {
    super(message);
    this.name = 'ConsumptionApiError';
  }
}

/**
 * Cliente de `GET /v1/public/sessions/current` e `POST /v1/orders/{orderId}/items/{itemId}/repeat`
 * (US-024/US-028), autenticado com o `sessionToken` anônimo emitido pelo esquema `TableSession`
 * (mesmo token salvo por `saveTableSession`, ver `session-storage.ts`).
 */
export class ConsumptionApi {
  constructor(
    private readonly sessionToken: string,
    private readonly baseUrl = '',
    // (...args: Parameters<typeof fetch>) => globalThis.fetch(...args): ver comentário em packages/ui/src/auth/operational-authenticated-fetch.ts
    // — `fetch` capturado bruto e chamado depois como `this.fetcher(...)` quebra em navegador real
    // ("Illegal invocation"), mascarado nos testes por injetarem um duplo.
    private readonly fetcher: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args),
  ) {}

  /**
   * SEM parâmetro de sessão na rota (US-024 §7) — a sessão é sempre a do próprio token
   * apresentado (ver docstring de `GetCurrentSessionConsumptionQuery` no backend). Isso é o que
   * garante, por construção, que o token da mesa 12 nunca consegue consultar a mesa 13
   * (ADR-021: 404, nunca 403).
   */
  async getCurrentConsumption(): Promise<SessionConsumptionResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/public/sessions/current`, {
      headers: { Accept: 'application/json', Authorization: `Bearer ${this.sessionToken}` },
    });
    await requireSuccess(response);
    return sessionConsumptionResponseSchema.parse(await response.json());
  }

  /**
   * US-027 §10 ("Cliente pode pré-visualizar a divisão no celular antes de o caixa começar") —
   * `GET /v1/public/sessions/current/bill`, mesmo raciocínio de rota SEM parâmetro de sessão de
   * {@link getCurrentConsumption}. Roda contra o edge (LAN da loja), não a nuvem — funciona mesmo
   * com a internet do estabelecimento fora do ar (local-first).
   */
  async getBillPreview(splitMode: 'BY_PERSON' | 'BY_ITEM' | 'BY_AMOUNT', people?: number): Promise<BillResponse> {
    const params = new URLSearchParams({ split: splitMode });
    if (people !== undefined) params.set('people', String(people));

    const response = await this.fetcher(`${this.baseUrl}/v1/public/sessions/current/bill?${params.toString()}`, {
      headers: { Accept: 'application/json', Authorization: `Bearer ${this.sessionToken}` },
    });
    await requireSuccess(response);
    return billResponseSchema.parse(await response.json());
  }

  /**
   * US-028 §7 — repetição em uma etapa, sem confirmação dupla (§10: "o atrito é justamente o que
   * a história elimina"). `Idempotency-Key` obrigatório (ADR-020) — gerado uma vez por toque
   * (`crypto.randomUUID()`), nunca reaproveitado entre toques distintos.
   */
  async repeatItem(orderId: string, itemId: string): Promise<{ unitPrice: string; repeatedFrom: string | null }> {
    const response = await this.fetcher(`${this.baseUrl}/v1/orders/${orderId}/items/${itemId}/repeat`, {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.sessionToken}`,
        'Idempotency-Key': cryptoRandomUuid(),
      },
    });
    await requireSuccess(response);
    const body = (await response.json()) as { item: { unitPrice: string; repeatedFromItemId: string | null } };
    return { unitPrice: body.item.unitPrice, repeatedFrom: body.item.repeatedFromItemId };
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string; code?: string } | null;
  throw new ConsumptionApiError(problem?.detail ?? 'Não foi possível concluir a operação.', problem?.code);
}

function cryptoRandomUuid(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  // Fallback só para ambiente de teste sem `crypto.randomUUID` (jsdom antigo) — nunca em produção.
  return `xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export type ConsumptionMode = 'ws' | 'polling';

export interface ConsumptionRealtimeCallbacks {
  readonly onEvent: (event: TableConsumptionEvent) => void;
  readonly onModeChange: (mode: ConsumptionMode) => void;
}

/**
 * ADR-011 (WebSocket local com fallback de polling): conecta ao hub `table-consumption` do edge
 * (esquema `TableSession`, token via querystring — o navegador não anexa header na conexão do
 * hub, mesmo motivo do `CatalogAvailabilityHub`/US-015) e cai para polling a cada 5 s
 * (`GET /v1/public/sessions/current`) se a conexão cair — sinalização visível do modo degradado
 * via `onModeChange` (consumido pelo componente `SyncStatus`).
 */
export class ConsumptionRealtimeConnection {
  private connection: signalR.HubConnection | null = null;
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private mode: ConsumptionMode = 'ws';
  private stopped = false;

  constructor(
    private readonly sessionToken: string,
    private readonly api: ConsumptionApi,
    private readonly callbacks: ConsumptionRealtimeCallbacks,
    private readonly baseUrl = '',
    private readonly pollIntervalMs = 5000,
  ) {}

  async start(): Promise<void> {
    this.stopped = false;

    // TUDO dentro do try — inclusive a construção do HubConnection: `.build()`/`withUrl` podem
    // lançar de forma SÍNCRONA (ex.: URL relativa não resolvível no ambiente de teste, sem
    // `window.location` real) e, dentro de uma função `async`, isso vira uma Promise rejeitada
    // não tratada se só o `await this.connection.start()` estivesse protegido. Qualquer falha
    // aqui — de construção OU de conexão — degrada para polling (ADR-011), nunca falha em
    // silêncio nem propaga um "unhandled rejection".
    try {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.baseUrl}/hubs/table-consumption`, {
          accessTokenFactory: () => this.sessionToken,
        })
        .withAutomaticReconnect([1000, 2000, 4000, 8000, 16000, 30000]) // ADR-011: backoff até 30 s
        .build();

      connection.on('tableConsumptionChanged', (payload: unknown) => {
        const parsed = tableConsumptionEventSchema.safeParse(payload);
        if (parsed.success) {
          this.callbacks.onEvent(parsed.data);
        }
      });

      connection.onreconnected(() => {
        this.stopPolling();
        this.setMode('ws');
      });

      connection.onclose(() => {
        if (!this.stopped) {
          this.startPolling();
        }
      });

      this.connection = connection;
      await connection.start();
      this.setMode('ws');
    } catch {
      // WebSocket indisponível (ex.: ambiente de teste sem hub real) — degrada para polling
      // imediatamente, nunca falha silenciosamente (ADR-011).
      this.startPolling();
    }
  }

  async stop(): Promise<void> {
    this.stopped = true;
    this.stopPolling();
    await this.connection?.stop();
  }

  private startPolling(): void {
    if (this.pollTimer) return;
    this.setMode('polling');
    this.pollTimer = setInterval(() => {
      void this.api.getCurrentConsumption().catch(() => {
        // Falha de polling não deve derrubar o app — próxima iteração tenta de novo.
      });
    }, this.pollIntervalMs);
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  private setMode(mode: ConsumptionMode): void {
    if (this.mode === mode) return;
    this.mode = mode;
    this.callbacks.onModeChange(mode);
  }
}
