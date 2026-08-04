import { HubConnectionBuilder, HttpTransportType, type HubConnection } from '@microsoft/signalr';

/**
 * Superfície mínima de `HubConnection` que este cliente precisa — permite testar sem abrir um
 * WebSocket de verdade em vitest/jsdom. Porte de `apps/web-pos/src/notifications/alerts-realtime.ts`
 * (mesmo hub `AlertsHub`, mesmo protocolo) — web-kds não tinha esse cliente antes de E-08, que
 * introduz a central de notificações (US-081) também na cozinha.
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
  /** Chamado a cada mensagem recebida no método `alert` (ver Hubs.AlertsHub) — inclui US-025/US-026 (chamada de garçom/conta, não usadas no KDS) e E-08 (`alert.raised`/`alert.group_updated`/`alert.resolved`). */
  readonly onAlert: (alert: AlertPayload) => void;
}

/**
 * Conecta ao `AlertsHub` — canal SEPARADO do `KdsHub` (kds-realtime.ts): aquele atualiza a FILA da
 * praça (sala por praça, todo terminal daquela praça recebe); este entrega só a quem precisa agir
 * (sala `role:{papel}`/`user:{id}`, derivada das claims do próprio token — o dispositivo nunca
 * escolhe a sala). E-08 (US-081) usa este canal para a central de notificações do shell
 * autenticado (ver notifications/use-notification-center.ts).
 *
 * [DECISÃO] Sem fallback de polling próprio (diferente de `KdsRealtimeClient`): o sinal
 * (som/vibração/badge do sino) é um REFORÇO sobre `GET /v1/notifications?status=unread`, que a
 * central já busca no boot e a cada evento. Se este canal cair, o pior caso é o alerta só aparecer
 * no próximo `refresh()` manual — `withAutomaticReconnect` (na fábrica de conexão real,
 * `createAlertsHubConnection`) já cobre a maioria das quedas transitórias.
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
