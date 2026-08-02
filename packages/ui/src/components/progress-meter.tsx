import type { HTMLAttributes, ReactNode } from 'react';

export interface ProgressMeterProps extends HTMLAttributes<HTMLDivElement> {
  readonly label?: ReactNode;
  readonly value?: number;
  readonly max?: number;
  /** Texto exibido à direita (já formatado). */
  readonly display?: ReactNode;
  readonly tone?: 'brand' | 'success' | 'warning' | 'danger' | 'accent';
  /** Posição da marca de meta, na mesma escala de `value`. */
  readonly target?: number;
  readonly caption?: ReactNode;
  readonly size?: 'md' | 'lg';
}

export function ProgressMeter({
  label,
  value = 0,
  max = 100,
  display,
  tone = 'brand',
  target,
  caption,
  size = 'md',
  className = '',
  ...rest
}: Readonly<ProgressMeterProps>) {
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  const cls = ['db-meter', size === 'lg' ? 'db-meter--lg' : '', className].filter(Boolean).join(' ');
  return (
    <div {...rest} className={cls}>
      {label || display ? (
        <div className="db-meter__top">
          <span className="db-meter__label">{label}</span>
          {display ? <span className="db-meter__value">{display}</span> : null}
        </div>
      ) : null}
      <div className="db-meter__track" role="meter" aria-valuenow={value} aria-valuemin={0} aria-valuemax={max}>
        <div className={`db-meter__fill db-meter__fill--${tone}`} style={{ width: `${pct}%` }} />
        {target != null ? (
          <span
            className="db-meter__mark"
            style={{ left: `${Math.max(0, Math.min(100, (target / max) * 100))}%` }}
          />
        ) : null}
      </div>
      {caption ? <span className="db-meter__caption">{caption}</span> : null}
    </div>
  );
}
