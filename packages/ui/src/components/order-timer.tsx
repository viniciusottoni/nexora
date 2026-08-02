import type { HTMLAttributes } from 'react';

/**
 * Cronômetro decorrido com escalonamento verde → amarelo → vermelho (RF-KDS-03).
 * Os limiares (`warnAt`, `lateAt`) são parâmetro do tenant, por produto — nunca fixe no código.
 */
export interface OrderTimerProps extends Omit<HTMLAttributes<HTMLSpanElement>, 'color'> {
  /** Segundos decorridos desde T0. */
  readonly seconds: number;
  /** Limiar de atenção em segundos. */
  readonly warnAt?: number;
  /** Limiar de atraso em segundos. */
  readonly lateAt?: number;
  readonly size?: 'sm' | 'md' | 'lg';
  readonly showIcon?: boolean;
  /** Use em `[data-surface="kds"]` — troca os fundos por versões escuras. */
  readonly onDark?: boolean;
}

type TimerState = 'ok' | 'warn' | 'late';

const FOREGROUND: Record<TimerState, string> = {
  ok: 'var(--nx-time-ok)',
  warn: 'var(--nx-time-warn)',
  late: 'var(--nx-time-late)',
};

const BACKGROUND_DARK: Record<TimerState, string> = {
  ok: 'var(--nx-time-ok-bg)',
  warn: 'var(--nx-time-warn-bg)',
  late: 'var(--nx-time-late-bg)',
};

const BACKGROUND_LIGHT: Record<TimerState, string> = {
  ok: 'var(--nx-success-100)',
  warn: 'var(--nx-warning-100)',
  late: 'var(--nx-danger-100)',
};

function formatElapsed(seconds: number): string {
  const sign = seconds < 0 ? '-' : '';
  const abs = Math.abs(seconds);
  const minutes = Math.floor(abs / 60);
  const remainder = abs % 60;
  return `${sign}${minutes}:${String(remainder).padStart(2, '0')}`;
}

function resolveState(seconds: number, warnAt: number, lateAt: number): TimerState {
  if (seconds >= lateAt) return 'late';
  if (seconds >= warnAt) return 'warn';
  return 'ok';
}

export function OrderTimer({
  seconds = 0,
  warnAt = 300,
  lateAt = 600,
  size = 'md',
  showIcon = false,
  onDark = false,
  className = '',
  style,
  ...props
}: Readonly<OrderTimerProps>) {
  const state = resolveState(seconds, warnAt, lateAt);
  const backgroundMap = onDark ? BACKGROUND_DARK : BACKGROUND_LIGHT;
  const color = FOREGROUND[state];
  const background = backgroundMap[state];
  return (
    <span
      {...props}
      data-state={state}
      className={`db-order-timer db-order-timer--${size} ${state === 'late' ? 'db-order-timer--late' : ''} ${className}`.trim()}
      style={{ color, background, ...style }}
    >
      {showIcon ? (
        <span aria-hidden="true" className="material-symbols-rounded db-order-timer__icon">
          timer
        </span>
      ) : null}
      {formatElapsed(seconds)}
    </span>
  );
}
