/** Linha de item em carrinho, comanda, conta do caixa ou comprovante. */
export interface OrderLineProps {
  qty: number;
  name: React.ReactNode;
  /** Adicionais e remoções, uma linha. */
  modifiers?: React.ReactNode;
  /** Observação livre do cliente (RF-PED-08). */
  note?: React.ReactNode;
  price?: React.ReactNode;
  /** Normalmente um `<StatusPill size="md" />`. */
  status?: React.ReactNode;
  actions?: React.ReactNode;
  cancelled?: boolean;
}
export function OrderLine(props: OrderLineProps): JSX.Element;
