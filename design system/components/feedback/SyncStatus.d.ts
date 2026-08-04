/**
 * Estado da conexão e do atraso de sincronização (RF-OFF-05 / RF-BI-14).
 * Obrigatório em toda tela operacional e no painel do dono: dado defasado
 * nunca pode ser apresentado como tempo real.
 */
export interface SyncStatusProps {
  state?: 'online' | 'local' | 'delayed';
  /** Ex. "há 4 s", "há 12 min". */
  lastSync?: string;
  /** Eventos pendentes na fila local. */
  queued?: number;
}
export function SyncStatus(props: SyncStatusProps): JSX.Element;
