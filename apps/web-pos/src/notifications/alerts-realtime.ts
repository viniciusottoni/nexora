import { HubConnectionBuilder, HttpTransportType, type HubConnection } from '@microsoft/signalr';

/**
 * Superfície mínima de `HubConnection` que este cliente precisa — mesmo espírito de
 * `TableMapHubConnection` (table-map-realtime.ts): permite testar sem abrir um WebSocket de
 * verdade em vitest/jsdom.
 */
export interface AlertsHubConnection {
  start(): Promise<void>;
  stop(): Promise<void>;
  on(methodName: string, callback: (payload: unknown) => void): void;
}

/** `{ type, data }` — mesmo formato de `SignalRAlertsBroadcaster` (backend). */
export interface AlertPayload {
  readonly type: string;
  readonly data: Record<string, unknown>;
}

export interface AlertsRealtimeOptions {
  /** Chamado a cada mensagem recebida no método `alert` (US-025/US-026, ver Hubs.AlertsHub). */
  readonly onAlert: (alert: AlertPayload) => void;
}

/**
 * Conecta ao `AlertsHub` (US-025 "Chamar garçom pela mesa", US-026 "Solicitar a conta") — canal
 * SEPARADO do `TableMapHub`: aquele atualiza o SNAPSHOT do mapa (sala por tenant, todo mundo
 * recebe); este entrega só a quem precisa agir (sala `role:{papel}`/`user:{id}`, derivada das
 * claims do próprio token — o dispositivo nunca escolhe a sala). Usado para acionar
 * vibração/som (US-025 §10) no exato momento em que o garçom responsável é chamado.
 *
 * [DECISÃO] Sem fallback de polling próprio (diferente de `TableMapRealtimeClient`): o efeito
 * sonoro é um REFORÇO sobre o indicador visual do mapa, que já tem seu próprio polling (ADR-011).
 * Se este canal específico cair, o pior caso é o garçom não sentir a vibração — ele ainda vê o
 * badge "Garçom chamado" no próximo `GET /v1/tables`. `withAutomaticReconnect` (na fábrica de
 * conexão real, `createAlertsHubConnection`) já cobre a maioria das quedas transitórias.
 */
export class AlertsRealtimeClient {
  constructor(
    private readonly connection: AlertsHubConnection,
    private readonly options: AlertsRealtimeOptions,
  ) {
    connection.on('alert', (payload) => {
      this.options.onAlert(payload as AlertPayload);
    });
  }

  async start(): Promise<void> {
    try {
      await this.connection.start();
    } catch {
      // Ver docstring da classe — este canal é reforço, não a única via do sinal.
    }
  }

  async stop(): Promise<void> {
    await this.connection.stop();
  }
}

/** Constrói o `HubConnection` real (produção) — separado da classe acima para ela ficar testável sem rede. */
export function createAlertsHubConnection(hubUrl: string, accessToken: () => string | Promise<string>): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: accessToken,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect([1000, 2000, 4000, 8000, 16000, 30000])
    .build();
}
