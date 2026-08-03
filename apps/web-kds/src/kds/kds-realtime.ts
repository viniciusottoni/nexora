import { HubConnectionBuilder, HttpTransportType, type HubConnection } from '@microsoft/signalr';
import type { KdsEvent } from '@nexora/contracts';

/** Estado de conexão exibido pelo `SyncStatus` (ADR-011): "ws" = tempo real, "polling" = degradado. */
export type KdsConnectionMode = 'ws' | 'polling';

/**
 * Superfície mínima de `HubConnection` que este cliente precisa — permite testar toda a lógica de
 * fallback/reconexão (US-031 §12) com um duplo de teste em vez de abrir um WebSocket de verdade em
 * vitest/jsdom. Mesmo padrão de `apps/web-pos/src/table-map/table-map-realtime.ts` (`TableMapHubConnection`),
 * com `invoke` adicional — este hub tem um método de cliente→servidor (`Resume`, ADR-011).
 */
export interface KdsHubConnection {
  start(): Promise<void>;
  stop(): Promise<void>;
  invoke(methodName: string, ...args: unknown[]): Promise<unknown>;
  on(methodName: string, callback: (payload: unknown) => void): void;
  onclose(callback: (error?: Error) => void): void;
  onreconnecting(callback: (error?: Error) => void): void;
  onreconnected(callback: (connectionId?: string) => void): void;
}

export interface KdsRealtimeOptions {
  /**
   * Chamado a cada mensagem `kdsEvent` recebida (tempo real OU replay de `Resume`) — a US-031
   * trata os dois como o MESMO sinal ("algo mudou na fila, busque o snapshot atual"), a mesma
   * decisão de "snapshot completo, não delta" documentada em `GetKdsQueueQueryHandler`/`KdsHub`:
   * o handler nunca tenta reconstruir o item a partir do payload do evento, só dispara um refetch.
   */
  readonly onEvent: (event: KdsEvent) => void;
  /** Chamado ao entrar/sair do modo degradado — liga o indicador de `SyncStatus`. */
  readonly onModeChange?: (mode: KdsConnectionMode) => void;
  /** Executado no fallback (a cada `pollIntervalMs`, padrão 5000 — ADR-011) e uma vez imediatamente ao cair. */
  readonly poll: () => void | Promise<void>;
  readonly pollIntervalMs?: number;
  /** `lastEventId` a informar em `Resume` na reconexão (ADR-011: "connection.invoke('Resume', { lastEventId })"). */
  readonly getLastEventId: () => string | undefined;
  readonly setIntervalFn?: typeof setInterval;
  readonly clearIntervalFn?: typeof clearInterval;
}

/**
 * WebSocket do KDS com fallback de polling e recuperação na reconexão (US-031, ADR-011) — mesma
 * máquina de estado de `TableMapRealtimeClient` (US-023), com o acréscimo do `Resume` chamado a
 * cada `onreconnected` (ver docstring de {@link KdsRealtimeOptions.onEvent} para por que o replay
 * de `Resume` e o push em tempo real convergem no mesmo callback).
 */
export class KdsRealtimeClient {
  private mode: KdsConnectionMode = 'ws';
  private pollHandle: ReturnType<typeof setInterval> | undefined;

  constructor(
    private readonly connection: KdsHubConnection,
    private readonly options: KdsRealtimeOptions,
  ) {
    connection.on('kdsEvent', (payload) => {
      this.options.onEvent(payload as KdsEvent);
    });
    connection.onclose(() => this.enterDegradedMode());
    connection.onreconnecting(() => this.enterDegradedMode());
    connection.onreconnected(() => {
      void this.resume();
      this.exitDegradedMode();
    });
  }

  get currentMode(): KdsConnectionMode {
    return this.mode;
  }

  async start(): Promise<void> {
    try {
      await this.connection.start();
      await this.resume();
    } catch {
      // ADR-011: WebSocket indisponível já na primeira tentativa cai no mesmo fallback de
      // polling — a cozinha não pode ficar sem nenhuma via.
      this.enterDegradedMode();
    }
  }

  async stop(): Promise<void> {
    this.stopPolling();
    await this.connection.stop();
  }

  /**
   * ADR-011 §"Reconexão com recuperação" — pede ao servidor (`KdsHub.Resume`) o que foi perdido
   * durante a queda. Nunca lança: uma falha aqui (ex.: hub ainda não respondeu ao handshake) não
   * pode impedir a conexão de seguir normal — o polling de fallback e o próximo `Resume` cobrem.
   */
  private async resume(): Promise<void> {
    try {
      await this.connection.invoke('Resume', this.options.getLastEventId());
    } catch {
      // Silencioso de propósito — ver comentário acima.
    }
  }

  private enterDegradedMode(): void {
    if (this.mode === 'polling') return;
    this.mode = 'polling';
    this.options.onModeChange?.('polling');
    void this.options.poll(); // não espera o primeiro tick de 5s para refletir a queda
    const setIntervalFn = this.options.setIntervalFn ?? setInterval;
    this.pollHandle = setIntervalFn(() => {
      void this.options.poll();
    }, this.options.pollIntervalMs ?? 5000);
  }

  private exitDegradedMode(): void {
    if (this.mode === 'ws') return;
    this.stopPolling();
    this.mode = 'ws';
    this.options.onModeChange?.('ws');
  }

  private stopPolling(): void {
    if (this.pollHandle === undefined) return;
    const clearIntervalFn = this.options.clearIntervalFn ?? clearInterval;
    clearIntervalFn(this.pollHandle);
    this.pollHandle = undefined;
  }
}

/** Constrói o `HubConnection` real (produção) — separado da classe acima para ela ficar testável sem rede. */
export function createKdsHubConnection(hubUrl: string, accessToken: () => string | Promise<string>): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: accessToken,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect([1000, 2000, 4000, 8000, 16000, 30000]) // ADR-011: backoff 1s,2s,4s,8s...teto 30s
    .build();
}
