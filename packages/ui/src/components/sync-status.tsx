import type { HTMLAttributes } from 'react';

/**
 * Estado da conexão e do atraso de sincronização (RF-OFF-05 / RF-BI-14).
 * Obrigatório em toda tela operacional e no painel do dono: dado defasado
 * nunca pode ser apresentado como tempo real.
 */
export type SyncStatusState = 'online' | 'local' | 'delayed';

export interface SyncStatusProps extends HTMLAttributes<HTMLSpanElement> {
  readonly state?: SyncStatusState;
  /** Ex. "há 4 s", "há 12 min". */
  readonly lastSync?: string;
  /** Eventos pendentes na fila local. */
  readonly queued?: number;
}

const STATE_TEXT: Record<SyncStatusState, readonly [string, string]> = {
  online: ['cloud_done', 'Sincronizado'],
  local: ['wifi_off', 'Modo local'],
  delayed: ['sync_problem', 'Sync atrasada'],
};

export function SyncStatus({
  state = 'online',
  lastSync,
  queued,
  className = '',
  title,
  ...props
}: Readonly<SyncStatusProps>) {
  const [iconName, label] = STATE_TEXT[state] ?? STATE_TEXT.online;
  return (
    <span
      {...props}
      className={`db-sync-status db-sync-status--${state} ${className}`.trim()}
      title={title ?? (lastSync ? `Última sincronização ${lastSync}` : undefined)}
    >
      <span aria-hidden="true" className="material-symbols-rounded db-sync-status__icon">
        {iconName}
      </span>
      {label}
      {lastSync ? <span className="db-sync-status__meta">{`· ${lastSync}`}</span> : null}
      {queued ? <span className="db-sync-status__meta">{`· ${queued} na fila`}</span> : null}
    </span>
  );
}
