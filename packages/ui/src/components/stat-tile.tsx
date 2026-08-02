import type { HTMLAttributes, ReactNode } from 'react';

export interface StatTileProps extends HTMLAttributes<HTMLDivElement> {
  readonly label: ReactNode;
  readonly value: ReactNode;
  /** Sufixo pequeno: "min", "%", "pedidos". */
  readonly unit?: string;
  /** Variação já formatada, ex. "+12,4%". */
  readonly delta?: string;
  readonly deltaDirection?: 'up' | 'down' | 'flat';
  /** Contra o que se compara, ex. "vs. mesma terça". */
  readonly comparison?: ReactNode;
  /** Meta do indicador, ex. "≤ 10 min". */
  readonly target?: ReactNode;
  /** Nome do ícone Material Symbols Rounded. */
  readonly icon?: string;
  readonly size?: 'md' | 'lg';
  /** `pulse` = fundo navy, para a faixa de tempo real do painel do dono. */
  readonly variant?: 'card' | 'flat' | 'pulse';
}

function materialIcon(name: string, size = 14) {
  return (
    <span
      aria-hidden="true"
      className="material-symbols-rounded"
      style={{ fontSize: `${size}px`, lineHeight: 1, flex: '0 0 auto' }}
    >
      {name}
    </span>
  );
}

export function StatTile({
  label,
  value,
  unit,
  delta,
  deltaDirection,
  comparison,
  target,
  icon,
  size = 'md',
  variant = 'card',
  className = '',
  ...rest
}: Readonly<StatTileProps>) {
  const direction =
    deltaDirection ?? (delta == null ? 'flat' : delta.trim().startsWith('-') ? 'down' : 'up');
  const cls = [
    'db-stat',
    size === 'lg' ? 'db-stat--lg' : '',
    variant === 'pulse' ? 'db-stat--pulse' : '',
    variant === 'flat' ? 'db-stat--flat' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');
  const hasFooter = delta != null || comparison != null || target != null;
  return (
    <div {...rest} className={cls}>
      <div className="db-stat__label">
        {icon ? materialIcon(icon) : null}
        {label}
      </div>
      <div className="db-stat__value">
        {value}
        {unit ? <span className="db-stat__unit">{unit}</span> : null}
      </div>
      {hasFooter ? (
        <div className="db-stat__footer">
          {delta != null ? (
            <span className={`db-stat__delta db-stat__delta--${direction}`}>
              {materialIcon(direction === 'up' ? 'arrow_upward' : direction === 'down' ? 'arrow_downward' : 'remove')}
              {delta}
            </span>
          ) : null}
          {comparison ? <span className="db-stat__comparison">{comparison}</span> : null}
          {target ? <span className="db-stat__target">meta {target}</span> : null}
        </div>
      ) : null}
    </div>
  );
}
