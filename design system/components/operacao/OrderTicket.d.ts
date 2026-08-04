/**
 * Cartão de pedido do KDS: código numérico grande, cronômetro escalonado,
 * itens com modificadores destacados e o "fire time" calculado.
 * Use dentro de `[data-surface="kds"]`.
 */
export interface OrderTicketItem {
  qty: number;
  name: React.ReactNode;
  /** Modificadores e observações — sempre em amarelo, é o que mais gera erro. */
  modifiers?: React.ReactNode;
  done?: boolean;
}
export interface OrderTicketProps {
  /** Código de dois/três dígitos digitado no teclado numérico. */
  code: React.ReactNode;
  /** Origem: "Mesa 12", "Delivery #4821". */
  where?: React.ReactNode;
  channel?: 'DINE_IN' | 'DELIVERY' | 'COUNTER';
  /** Segundos desde T0. */
  seconds?: number;
  warnAt?: number;
  lateAt?: number;
  items: OrderTicketItem[];
  /** Momento de iniciar para saída sincronizada (RF-KDS-09), ex. "em 3 min". */
  fireAt?: string;
  footer?: React.ReactNode;
  onDark?: boolean;
}
export function OrderTicket(props: OrderTicketProps): JSX.Element;
