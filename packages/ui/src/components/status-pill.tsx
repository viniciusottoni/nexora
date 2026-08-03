import type { HTMLAttributes, ReactNode } from 'react';

/**
 * Estado da máquina de estado do pedido, mesa, caixa ou entrega (doc. 04).
 * Um estado tem UMA cor fixa em todo o ecossistema — nunca reatribua cor por tela.
 */
export type StatusPillStatus =
  | 'FREE'
  | 'OCCUPIED'
  | 'RESERVED'
  | 'BLOCKED'
  | 'OPEN'
  | 'QUEUED'
  | 'FIRED'
  | 'IN_OVEN'
  | 'OUT_OF_OVEN'
  | 'READY'
  | 'SERVED'
  | 'BILL_REQUESTED'
  | 'PAID'
  | 'CLOSED'
  | 'DISPATCHED'
  | 'DELIVERED'
  | 'CANCELLED'
  | 'LATE'
  | 'UNAVAILABLE';

export interface StatusPillProps extends Omit<HTMLAttributes<HTMLSpanElement>, 'color'> {
  readonly status: StatusPillStatus;
  /** Sobrescreve o rótulo canônico (use com parcimônia). */
  readonly label?: ReactNode;
  readonly size?: 'md' | 'lg';
  /** Ponto pulsante — só para estado que exige ação agora. */
  readonly live?: boolean;
}

/** Rótulo e par de cores (texto, fundo) canônicos por estado. Não reatribuir. */
const STATUS_MAP: Record<StatusPillStatus, readonly [string, string, string]> = {
  FREE: ['Livre', 'var(--text-secondary)', 'var(--surface-sunken)'],
  OCCUPIED: ['Ocupada', 'var(--nx-blue-600)', 'var(--nx-blue-100)'],
  RESERVED: ['Reservada', 'var(--nx-navy-700)', 'var(--surface-brand-subtle)'],
  BLOCKED: ['Bloqueada', 'var(--nx-danger-600)', 'var(--nx-danger-100)'],
  OPEN: ['Ocupada', 'var(--nx-blue-600)', 'var(--nx-blue-100)'],
  QUEUED: ['Na fila', 'var(--text-secondary)', 'var(--surface-sunken)'],
  FIRED: ['Em produção', 'var(--nx-warning-600)', 'var(--nx-warning-100)'],
  IN_OVEN: ['No forno', 'var(--nx-warning-600)', 'var(--nx-warning-100)'],
  OUT_OF_OVEN: ['Fora do forno', 'var(--nx-cyan-600)', 'var(--nx-cyan-100)'],
  READY: ['Pronto', 'var(--nx-success-600)', 'var(--nx-success-100)'],
  SERVED: ['Entregue', 'var(--nx-teal-600)', 'var(--nx-teal-100)'],
  BILL_REQUESTED: ['Conta pedida', 'var(--nx-navy-700)', 'var(--surface-brand-subtle)'],
  PAID: ['Pago', 'var(--nx-success-600)', 'var(--nx-success-100)'],
  CLOSED: ['Fechada', 'var(--text-secondary)', 'var(--surface-sunken)'],
  DISPATCHED: ['Em rota', 'var(--nx-cyan-600)', 'var(--nx-cyan-100)'],
  DELIVERED: ['Entregue', 'var(--nx-success-600)', 'var(--nx-success-100)'],
  CANCELLED: ['Cancelado', 'var(--nx-danger-600)', 'var(--nx-danger-100)'],
  LATE: ['Atrasado', 'var(--nx-danger-600)', 'var(--nx-danger-100)'],
  UNAVAILABLE: ['Em falta', 'var(--nx-danger-600)', 'var(--nx-danger-100)'],
};

const FALLBACK: readonly [string, string, string] = ['—', 'var(--text-secondary)', 'var(--surface-sunken)'];

export function StatusPill({
  status,
  label,
  size = 'md',
  live = false,
  className = '',
  style,
  ...props
}: Readonly<StatusPillProps>) {
  const [canonicalLabel, color, background] = STATUS_MAP[status] ?? FALLBACK;
  return (
    <span
      {...props}
      className={`db-status-pill ${size === 'lg' ? 'db-status-pill--lg' : ''} ${live ? 'db-status-pill--live' : ''} ${className}`.trim()}
      style={{ color, background, ...style }}
    >
      <span className="db-status-pill__dot" />
      {label ?? canonicalLabel}
    </span>
  );
}
