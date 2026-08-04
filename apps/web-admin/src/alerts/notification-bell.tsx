import { useCallback, useEffect, useState } from 'react';
import { NotificationCenter, type NotificationCenterItem } from '@nexora/ui';
import type { Alert } from '@nexora/contracts';
import { NotificationsApi } from './notifications-api.js';

/**
 * Sino de notificações do `TopBar` (US-081 §3/§10) — só o suficiente para o gestor não perder um
 * alerta pendente sem abrir a central completa (essa é responsabilidade de outro app/tarefa em
 * `web-pos`/`web-kds`). Usa o `NotificationCenter` pronto de `@nexora/ui` (100% controlado); este
 * componente só busca `GET /v1/notifications?status=unread`, reconhece via
 * `POST /v1/alerts/{id}/acknowledge` e mantém a lista fresca por polling.
 *
 * Polling simples de 30 s em vez de WebSocket/SignalR: o web-admin fala com a nuvem
 * (`CLOUD_API_BASE_URL`), não com o WebSocket local do edge que entrega em <2s (US-081 §9) — esse
 * canal é para quem está na loja (POS/KDS). Documentado aqui de propósito, não é o canal final.
 */
const POLL_INTERVAL_MS = 30_000;

function toItem(alert: Alert): NotificationCenterItem {
  return {
    id: alert.id,
    type: alert.type,
    severity: alert.severity,
    message: alert.message,
    raisedAt: alert.raisedAt,
    acknowledgedAt: alert.acknowledgedAt,
  };
}

export interface NotificationBellProps {
  /** Injetável para teste — padrão `new NotificationsApi()`. */
  readonly notificationsApi?: NotificationsApi;
}

export function NotificationBell({
  notificationsApi = new NotificationsApi(),
}: Readonly<NotificationBellProps>) {
  const [alerts, setAlerts] = useState<readonly Alert[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  const refresh = useCallback(() => {
    setLoading(true);
    notificationsApi
      .listUnread()
      .then(setAlerts)
      .catch(() => {
        // Falha de polling é silenciosa — o sino simplesmente não atualiza até a próxima tentativa,
        // sem interromper a operação do gestor (US-081 §10: "nunca bloqueia a tela").
      })
      .finally(() => setLoading(false));
  }, [notificationsApi]);

  useEffect(() => {
    refresh();
    const interval = setInterval(refresh, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [refresh]);

  async function acknowledge(id: string) {
    setAlerts((current) =>
      current.map((alert) =>
        alert.id === id ? { ...alert, acknowledgedAt: new Date().toISOString() } : alert,
      ),
    );
    try {
      await notificationsApi.acknowledge(id);
    } finally {
      refresh();
    }
  }

  return (
    <NotificationCenter
      items={alerts.map(toItem)}
      open={open}
      onOpenChange={setOpen}
      onAcknowledge={(id) => void acknowledge(id)}
      loading={loading && alerts.length === 0}
    />
  );
}
