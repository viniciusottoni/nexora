import type { HTMLAttributes, ReactNode } from 'react';
import { Icon } from './icon.js';

/** Linha de item em carrinho, comanda, conta do caixa ou comprovante. */
export interface OrderLineProps extends HTMLAttributes<HTMLDivElement> {
  readonly qty: number;
  readonly name: ReactNode;
  /** Adicionais e remoções, uma linha. */
  readonly modifiers?: ReactNode;
  /** Observação livre do cliente (RF-PED-08). */
  readonly note?: ReactNode;
  readonly price?: ReactNode;
  /** Normalmente um `<StatusPill size="md" />`. */
  readonly status?: ReactNode;
  readonly actions?: ReactNode;
  readonly cancelled?: boolean;
}

export function OrderLine({
  qty,
  name,
  modifiers,
  note,
  price,
  status,
  actions,
  cancelled = false,
  className = '',
  ...rest
}: Readonly<OrderLineProps>) {
  return (
    <div {...rest} className={`db-order-line ${cancelled ? 'db-order-line--cancelled' : ''} ${className}`.trim()}>
      <span className="db-order-line__qty">{qty}×</span>
      <span className="db-order-line__body">
        <span className="db-order-line__name">{name}</span>
        {modifiers ? (
          <span className="db-order-line__meta" style={{ display: 'block' }}>
            {modifiers}
          </span>
        ) : null}
        {note ? (
          <span className="db-order-line__meta" style={{ display: 'flex', alignItems: 'center', gap: '3px' }}>
            <Icon name="edit_note" size={14} />
            {note}
          </span>
        ) : null}
        {status ? <span className="db-order-line__status">{status}</span> : null}
      </span>
      {price ? <span className="db-order-line__price">{price}</span> : null}
      {actions ? <span className="db-order-line__actions">{actions}</span> : null}
    </div>
  );
}
