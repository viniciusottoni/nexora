import { HubConnectionBuilder, HttpTransportType, type HubConnection } from '@microsoft/signalr';
import type { TableMapEntry } from '@nexora/contracts';

/** Estado de conexão exibido pelo `SyncStatus` (ADR-011): "ws" = tempo real, "polling" = degradado. */
export type TableMapConnectionMode = 'ws' | 'polling';

/**
 * Superfície mínima de `HubConnection` que este cliente precisa — permite testar toda a lógica de
 * fallback (US-023 §12, "fallback de polling entrega em no máximo 5 s") com um duplo de teste em
 * vez de abrir um WebSocket de verdade em vitest/jsdom.
 */
export interface TableMapHubConnection {
  start(): Promise<void>;
  stop(): Promise<void>;
  on(methodName: string, callback: (payload: unknown) => void): void;
  onclose(callback: (error?: Error) => void): void;
  onreconnecting(callback: (error?: Error) => void): void;
  onreconnected(callback: (connectionId?: string) => void): void;
}

export interface TableMapRealtimeOptions {
  /** Chamado a cada evento `table.changed` recebido pelo WebSocket. */
  readonly onTableChanged: (table: TableMapEntry) => void;
  /** Chamado ao entrar/sair do modo degradado — liga o indicador de `SyncStatus`. */
  readonly onModeChange?: (mode: TableMapConnectionMode) => void;
  /**
   * Executado no fallback (a cada `pollIntervalMs`, padrão 5000 — ADR-011 "Intervalo de
   * polling: 5 s") e uma vez imediatamente ao entrar em modo degradado, para não esperar o
   * primeiro tick inteiro antes de refletir a mudança que derrubou o WebSocket.
   */
  readonly poll: () => void | Promise<void>;
  readonly pollIntervalMs?: number;
  readonly setIntervalFn?: typeof setInterval;
  readonly clearIntervalFn?: typeof clearInterval;
}

/**
 * WebSocket do mapa de mesas com fallback de polling (ADR-011) — a mesma máquina de estado
 * descrita no ADR (`mode: 'ws' | 'polling'`), independente de qual transporte concreto está por
 * trás de {@link TableMapHubConnection} (produção: SignalR real; teste: duplo controlado).
 */
export class TableMapRealtimeClient {
  private mode: TableMapConnectionMode = 'ws';
  private pollHandle: ReturnType<typeof setInterval> | undefined;

  constructor(
    private readonly connection: TableMapHubConnection,
    private readonly options: TableMapRealtimeOptions,
  ) {
    connection.on('table.changed', (payload) => {
      this.options.onTableChanged(payload as TableMapEntry);
    });
    connection.onclose(() => this.enterDegradedMode());
    connection.onreconnecting(() => this.enterDegradedMode());
    connection.onreconnected(() => this.exitDegradedMode());
  }

  get currentMode(): TableMapConnectionMode {
    return this.mode;
  }

  async start(): Promise<void> {
    try {
      await this.connection.start();
    } catch {
      // ADR-011: WebSocket indisponível já na primeira tentativa (não só numa queda depois de
      // conectado) cai no mesmo fallback de polling — o salão não pode ficar sem nenhuma via.
      this.enterDegradedMode();
    }
  }

  async stop(): Promise<void> {
    this.stopPolling();
    await this.connection.stop();
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
export function createTableMapHubConnection(hubUrl: string, accessToken: () => string | Promise<string>): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: accessToken,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect([1000, 2000, 4000, 8000, 16000, 30000]) // ADR-011: backoff 1s,2s,4s,8s...teto 30s
    .build();
}
