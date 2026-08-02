import type { HTMLAttributes, ReactNode } from 'react';
import { Icon } from './icon.js';
import { OrderTimer } from './order-timer.js';

/** Canais de origem exibidos no rodapé do ticket. */
type OrderTicketChannel = 'DINE_IN' | 'DELIVERY' | 'COUNTER';

export interface OrderTicketItem {
  readonly qty: number;
  readonly name: ReactNode;
  /** Modificadores e observações — sempre em amarelo, é o que mais gera erro. */
  readonly modifiers?: ReactNode;
  readonly done?: boolean;
}

export interface OrderTicketProps extends HTMLAttributes<HTMLElement> {
  /** Código de dois/três dígitos digitado no teclado numérico. */
  readonly code: ReactNode;
  /** Origem: "Mesa 12", "Delivery #4821". */
  readonly where?: ReactNode;
  readonly channel?: OrderTicketChannel;
  /** Segundos desde T0. */
  readonly seconds?: number;
  readonly warnAt?: number;
  readonly lateAt?: number;
  readonly items: readonly OrderTicketItem[];
  /** Momento de iniciar para saída sincronizada (RF-KDS-09), ex. "em 3 min". */
  readonly fireAt?: string;
  readonly footer?: ReactNode;
  readonly onDark?: boolean;
}

const CHANNEL_LABEL: Record<OrderTicketChannel, string> = {
  DINE_IN: 'Salão',
  DELIVERY: 'Delivery',
  COUNTER: 'Balcão',
};

const CHANNEL_ICON: Record<OrderTicketChannel, string> = {
  DINE_IN: 'table_restaurant',
  DELIVERY: 'delivery_dining',
  COUNTER: 'takeout_dining',
};

function isPrimitive(value: ReactNode): value is string | number {
  return typeof value === 'string' || typeof value === 'number';
}

/** Chave estável sem depender da posição no array: cai em `n${index}`/`m${index}`
 * só quando `name`/`modifiers` não são texto simples (ex. JSX), caso raro. */
function itemKey(item: OrderTicketItem, index: number): string {
  const namePart = isPrimitive(item.name) ? String(item.name) : `n${index}`;
  const modifiersPart = isPrimitive(item.modifiers) ? String(item.modifiers) : '';
  return `${item.qty}×${namePart}×${modifiersPart}`;
}

export function OrderTicket({
  code,
  where,
  channel,
  seconds = 0,
  warnAt = 300,
  lateAt = 600,
  items = [],
  fireAt,
  footer,
  onDark = true,
  className = '',
  ...rest
}: Readonly<OrderTicketProps>) {
  const late = seconds >= lateAt;
  return (
    <article {...rest} className={`db-order-ticket ${late ? 'db-order-ticket--late' : ''} ${className}`.trim()}>
      <div className="db-order-ticket__head">
        <div className="db-order-ticket__id">
          <span className="db-order-ticket__code">{code}</span>
          <span className="db-order-ticket__where">{where}</span>
        </div>
        <OrderTimer seconds={seconds} warnAt={warnAt} lateAt={lateAt} size="md" onDark={onDark} />
      </div>
      <ul className="db-order-ticket__items">
        {items.map((item, index) => (
          <li
            key={itemKey(item, index)}
            className={`db-order-ticket__item ${item.done ? 'db-order-ticket__item--done' : ''}`.trim()}
          >
            <span className="db-order-ticket__qty">{item.qty}×</span>
            <span>
              <div className="db-order-ticket__name">{item.name}</div>
              {item.modifiers ? <div className="db-order-ticket__modifiers">{item.modifiers}</div> : null}
            </span>
          </li>
        ))}
      </ul>
      {footer || channel || fireAt ? (
        <div className="db-order-ticket__foot">
          {channel ? (
            <span className="db-order-ticket__channel">
              <Icon name={CHANNEL_ICON[channel]} size={16} />
              {CHANNEL_LABEL[channel]}
            </span>
          ) : null}
          {footer}
          {fireAt ? (
            <span className="db-order-ticket__fire">
              <Icon name="local_fire_department" size={16} />
              {'montar ' + fireAt}
            </span>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}
