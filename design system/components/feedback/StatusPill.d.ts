/**
 * Estado de máquina de estado, com rótulo e cor canônicos (doc 04 › §4).
 * Um estado = uma cor em todo o ecossistema; nunca reatribua cores.
 */
export interface StatusPillProps {
  status:
    | 'FREE' | 'OPEN' | 'QUEUED' | 'FIRED' | 'IN_OVEN' | 'OUT_OF_OVEN' | 'READY'
    | 'SERVED' | 'BILL_REQUESTED' | 'PAID' | 'CLOSED' | 'DISPATCHED'
    | 'DELIVERED' | 'CANCELLED' | 'LATE' | 'UNAVAILABLE';
  /** Sobrescreve o rótulo canônico (use com parcimônia). */
  label?: React.ReactNode;
  size?: 'md' | 'lg';
  /** Ponto pulsante — só para estado que exige ação agora. */
  live?: boolean;
}
export function StatusPill(props: StatusPillProps): JSX.Element;
