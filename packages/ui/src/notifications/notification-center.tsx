import { useEffect, useRef } from 'react';
import { Button } from '../components/button.js';
import { EmptyState } from '../components/empty-state.js';
import { Icon } from '../components/icon.js';
import { IconButton } from '../components/icon-button.js';

export type NotificationSeverity = 'INFO' | 'WARNING' | 'HIGH' | 'CRITICAL';

/**
 * Um item da central de notificações (E-08/US-081 §3) — tanto um alerta individual quanto um GRUPO
 * (US-083, `count > 1`). O app que consome este componente decide se lista alertas crus ou
 * agrupados (`GET /v1/alerts?grouped=true`); o componente só apresenta o que recebe.
 */
export interface NotificationCenterItem {
  readonly id: string;
  readonly type: string;
  readonly severity: NotificationSeverity;
  readonly message: string;
  readonly raisedAt: string;
  readonly acknowledgedAt?: string | null;
  /** >1 quando este item representa um grupo (US-083 §4: "5 pedidos atrasados"). */
  readonly count?: number;
}

export interface NotificationCenterProps {
  readonly items: readonly NotificationCenterItem[];
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  /** Ausente = item sem ação de reconhecer (ex.: linha de um grupo, reconhecido pelo item pai). */
  readonly onAcknowledge?: (id: string) => void;
  readonly loading?: boolean;
  /**
   * US-081 §4, cenário "Permissão não concedida": "deve haver convite discreto para ativar as
   * notificações" — `false` mostra o convite, `true`/`undefined` (permissão já respondida ou
   * push não suportado neste navegador) oculta.
   */
  readonly pushPermissionPending?: boolean;
  readonly onRequestPushPermission?: () => void;
}

const SEVERITY_ICON: Record<NotificationSeverity, string> = {
  INFO: 'info',
  WARNING: 'warning',
  HIGH: 'error',
  CRITICAL: 'crisis_alert',
};

const SEVERITY_LABEL: Record<NotificationSeverity, string> = {
  INFO: 'Informativo',
  WARNING: 'Atenção',
  HIGH: 'Importante',
  CRITICAL: 'Crítico',
};

function formatRelativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.max(0, Math.round(diffMs / 60_000));
  if (minutes < 1) return 'agora';
  if (minutes < 60) return `há ${minutes} min`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `há ${hours} h`;
  return `há ${Math.round(hours / 24)} d`;
}

/**
 * Central de notificações (US-081 §3/§10) — sino com contador de pendentes + painel com
 * histórico. Nunca bloqueia a tela (US-081 §10: "alerta in-app nunca bloqueia a operação") — é um
 * painel ancorado, não um `Modal`/portal: o gatilho sempre vive na `TopBar` (chrome fixo de topo,
 * fora de qualquer `Card` com `overflow`), então não há o risco de recorte que levou `Modal` a
 * usar portal (ver CLAUDE.md § Motion).
 */
export function NotificationCenter({
  items,
  open,
  onOpenChange,
  onAcknowledge,
  loading = false,
  pushPermissionPending = false,
  onRequestPushPermission,
}: Readonly<NotificationCenterProps>) {
  const containerRef = useRef<HTMLDivElement>(null);
  const pendingCount = items.filter((item) => !item.acknowledgedAt).length;
  const highestPendingSeverity = items
    .filter((item) => !item.acknowledgedAt)
    .reduce<NotificationSeverity | null>((max, item) => {
      const order: NotificationSeverity[] = ['INFO', 'WARNING', 'HIGH', 'CRITICAL'];
      if (max === null || order.indexOf(item.severity) > order.indexOf(max)) return item.severity;
      return max;
    }, null);

  useEffect(() => {
    if (!open) return undefined;
    function handlePointerDown(event: PointerEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        onOpenChange(false);
      }
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onOpenChange(false);
    }
    document.addEventListener('pointerdown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [open, onOpenChange]);

  return (
    <div className="db-notification-center" ref={containerRef}>
      <IconButton
        icon="notifications"
        label={pendingCount > 0 ? `Notificações — ${pendingCount} pendentes` : 'Notificações'}
        {...(pendingCount > 0 ? { badge: pendingCount > 99 ? '99+' : pendingCount } : {})}
        className={highestPendingSeverity === 'CRITICAL' || highestPendingSeverity === 'HIGH' ? 'db-notification-center__trigger--urgent' : ''}
        aria-expanded={open}
        onClick={() => onOpenChange(!open)}
      />
      {open ? (
        <div className="db-notification-center__panel nx-anim-toast-in" role="dialog" aria-label="Central de notificações">
          <div className="db-notification-center__header">
            <span className="db-notification-center__title">Notificações</span>
            {pendingCount > 0 ? <span className="db-notification-center__count">{pendingCount} pendente(s)</span> : null}
          </div>

          {pushPermissionPending ? (
            <button type="button" className="db-notification-center__push-invite" onClick={onRequestPushPermission}>
              <Icon name="notifications_active" size={16} />
              Ativar notificações no navegador para não perder alertas com o sistema fechado
            </button>
          ) : null}

          <div className="db-notification-center__list nx-stagger">
            {loading ? (
              <div className="db-notification-center__loading">
                <span className="nx-spinner" aria-hidden="true" />
                Carregando…
              </div>
            ) : items.length === 0 ? (
              <EmptyState icon="notifications_off" title="Nenhuma notificação">
                Você será avisado aqui assim que algo precisar da sua atenção.
              </EmptyState>
            ) : (
              items.map((item) => (
                <div
                  key={item.id}
                  className={`db-notification-center__item db-notification-center__item--${item.severity.toLowerCase()} ${
                    item.acknowledgedAt ? 'db-notification-center__item--read' : ''
                  }`.trim()}
                >
                  <span aria-hidden="true" className="material-symbols-rounded db-notification-center__item-icon">
                    {SEVERITY_ICON[item.severity]}
                  </span>
                  <div className="db-notification-center__item-body">
                    <div className="db-notification-center__item-message">
                      {item.count && item.count > 1 ? <strong>{item.count}× </strong> : null}
                      {item.message}
                    </div>
                    <div className="db-notification-center__item-meta">
                      <span>{SEVERITY_LABEL[item.severity]}</span>
                      <span>{formatRelativeTime(item.raisedAt)}</span>
                    </div>
                  </div>
                  {!item.acknowledgedAt && onAcknowledge ? (
                    <Button size="sm" variant="ghost" onClick={() => onAcknowledge(item.id)}>
                      Reconhecer
                    </Button>
                  ) : null}
                </div>
              ))
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
