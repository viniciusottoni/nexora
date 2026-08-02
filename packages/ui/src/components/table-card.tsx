import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { Icon } from './icon.js';
import { StatusPill } from './status-pill.js';

export interface TableCardProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'name'> {
  /** Identificação curta, ex. "Mesa 12" ou "M12". */
  readonly name: ReactNode;
  readonly status?: 'FREE' | 'OPEN' | 'BILL_REQUESTED' | 'PAID' | 'READY' | 'CLOSED';
  /** Tempo desde a abertura, já formatado ("42 min"). */
  readonly elapsed?: string;
  readonly guests?: number;
  /** Consumo acumulado formatado ("R$ 186,40"). */
  readonly total?: ReactNode;
  readonly waiter?: string;
  /** Contorno vermelho + pulso: mesa exige ação agora (chamou garçom, pediu conta). */
  readonly attention?: boolean;
}

export const TableCard = forwardRef<HTMLButtonElement, Readonly<TableCardProps>>(function TableCard(
  { name, status = 'FREE', elapsed, guests, total, waiter, attention = false, className = '', type = 'button', ...rest },
  ref,
) {
  return (
    <button
      {...rest}
      ref={ref}
      type={type}
      className={`db-table-card ${status === 'FREE' ? 'db-table-card--free' : ''} ${attention ? 'db-table-card--attention' : ''} ${className}`.trim()}
    >
      <div className="db-table-card__top">
        <span className="db-table-card__name">{name}</span>
        <StatusPill status={status} live={attention} />
      </div>
      <div className="db-table-card__meta">
        {guests ? (
          <span>
            <Icon name="group" size={14} />
            {guests}
          </span>
        ) : null}
        {elapsed ? (
          <span>
            <Icon name="schedule" size={14} />
            {elapsed}
          </span>
        ) : null}
        {waiter ? (
          <span>
            <Icon name="room_service" size={14} />
            {waiter}
          </span>
        ) : null}
      </div>
      {total ? <div className="db-table-card__total">{total}</div> : null}
    </button>
  );
});
