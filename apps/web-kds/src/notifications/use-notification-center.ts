import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { Alert } from '@nexora/contracts';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { playAlertChime, vibrateAlert } from './alert-sound.js';
import { AlertsRealtimeClient, createAlertsHubConnection, type AlertPayload } from './alerts-realtime.js';
import { NotificationCenterApi } from './notification-center-api.js';
import { subscribeToPush } from './push-notifications.js';

export interface UseNotificationCenterOptions {
  /** Vazio no edge (mesma origem) — parâmetro só para permitir apontar para outro host em teste. */
  readonly baseUrl?: string;
  /**
   * `undefined` antes do login (dispositivo ainda não pareado / operador ainda não autenticado) —
   * o hook fica ocioso (sem fetch, sem conexão realtime) até a identidade existir. Sempre chamado
   * de forma incondicional pelo componente (regra dos hooks); é este campo, não a chamada do hook,
   * que reflete a tela ainda não autenticada (mesmo padrão do efeito de `configurePosOrderQueue` em
   * `app.tsx`: `if (!device || !session) return;` dentro do efeito, nunca em volta do hook).
   */
  readonly identity: Readonly<OperationalRequestIdentity> | undefined;
  readonly api?: NotificationCenterApi;
}

export interface UseNotificationCenterResult {
  readonly items: readonly Alert[];
  readonly loading: boolean;
  readonly error: string | undefined;
  readonly acknowledge: (id: string) => Promise<void>;
  readonly refresh: () => Promise<void>;
  /** US-081 §4/§10 — `true` quando vale mostrar o convite discreto de push (ver `NotificationCenter`). */
  readonly pushPermissionPending: boolean;
  readonly requestPushPermission: () => Promise<void>;
}

function readNotificationPermission(): NotificationPermission {
  return typeof Notification === 'undefined' ? 'denied' : Notification.permission;
}

/**
 * Camada de dados da central de notificações (US-081, US-083) — combina:
 * 1) carga inicial via `GET /v1/notifications?status=unread` (`NotificationCenterApi`);
 * 2) tempo real via `AlertsHub` (`alert.raised`/`alert.group_updated`/`alert.resolved`), SEMPRE
 *    reagindo com um `refresh()` (nunca reconstrói o item localmente a partir do payload do evento
 *    — o servidor é a fonte da verdade da mensagem já consolidada, US-083 §10);
 * 3) o convite discreto de push, oferecido só depois que ESTE dispositivo já viu pelo menos um
 *    alerta na sessão corrente (US-081 §10: "pedida no momento em que o valor está claro, não no
 *    primeiro acesso") — nunca no boot do app.
 *
 * Som/vibração (`playAlertChime`/`vibrateAlert`) tocam SÓ em `alert.raised` — `alert.group_updated`
 * (um alerta a mais se juntando a um grupo já aberto) atualiza a contagem silenciosamente (US-083
 * §10: "não deve haver nova notificação sonora").
 */
export function useNotificationCenter({
  baseUrl = '',
  identity,
  api,
}: UseNotificationCenterOptions): UseNotificationCenterResult {
  const notificationCenterApi = useMemo(() => api ?? new NotificationCenterApi(baseUrl), [api, baseUrl]);
  const [items, setItems] = useState<readonly Alert[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [permission, setPermission] = useState<NotificationPermission>(readNotificationPermission);
  // Ref (não state): só marca "já vimos um alerta nesta sessão" para liberar o convite de push — não
  // deve disparar re-render por si só (o convite reage à mudança de `hasReceivedAlert.current` via o
  // próprio evento que já causa outro re-render, o `setItems`/`setPermission` correspondente).
  const hasReceivedAlertRef = useRef(false);
  const [hasReceivedAlert, setHasReceivedAlert] = useState(false);

  const refresh = useCallback(async () => {
    if (!identity) return;
    try {
      const response = await notificationCenterApi.listUnread(identity);
      setItems(response.alerts);
      if (response.alerts.length > 0 && !hasReceivedAlertRef.current) {
        hasReceivedAlertRef.current = true;
        setHasReceivedAlert(true);
      }
      setError(undefined);
    } catch {
      // Mantém a última lista conhecida — a central de notificações não pode "sumir" por uma falha
      // de rede transitória (mesmo espírito de TableMapPage/KdsQueuePage).
      setError('Sem conexão com o servidor local — mostrando as últimas notificações conhecidas.');
    } finally {
      setLoading(false);
    }
  }, [notificationCenterApi, identity]);

  // Ref para a versão mais recente de `refresh` — o efeito da conexão realtime (abaixo) só
  // reconecta o WebSocket se accessToken mudar, nunca por causa desta closure.
  const refreshRef = useRef(refresh);
  useEffect(() => {
    refreshRef.current = refresh;
  }, [refresh]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (!identity) return undefined;
    const connection = createAlertsHubConnection(`${baseUrl}/hubs/alerts`, () => identity.accessToken);
    const realtime = new AlertsRealtimeClient(connection, {
      onAlert: (alert: AlertPayload) => {
        switch (alert.type) {
          case 'alert.raised':
            vibrateAlert();
            playAlertChime();
            hasReceivedAlertRef.current = true;
            setHasReceivedAlert(true);
            void refreshRef.current();
            break;
          case 'alert.group_updated':
          case 'alert.resolved':
            // US-083 §10: NUNCA soa de novo aqui — só atualiza a contagem/mensagem consolidada.
            void refreshRef.current();
            break;
          default:
            break;
        }
      },
    });
    void realtime.start();
    return () => {
      void realtime.stop();
    };
  }, [baseUrl, identity?.accessToken]);

  const acknowledge = useCallback(
    async (id: string) => {
      if (!identity) return;
      const updated = await notificationCenterApi.acknowledge(identity, id);
      // Mantém o item na lista (marcado como lido) em vez de removê-lo — a central mostra HISTÓRICO
      // com estado (US-081 §4, cenário "Central de notificações"), não só os pendentes; ele some da
      // lista no próximo `refresh()` natural, quando o backend deixa de devolvê-lo em `status=unread`.
      setItems((current) => current.map((item) => (item.id === id ? updated : item)));
    },
    [notificationCenterApi, identity],
  );

  const requestPushPermission = useCallback(async () => {
    if (typeof Notification === 'undefined' || !identity) return;
    const result = await Notification.requestPermission();
    setPermission(result);
    if (result === 'granted') {
      await subscribeToPush({ identity });
    }
  }, [identity]);

  const pushPermissionPending =
    hasReceivedAlert && permission === 'default' && typeof Notification !== 'undefined';

  return { items, loading, error, acknowledge, refresh, pushPermissionPending, requestPushPermission };
}
