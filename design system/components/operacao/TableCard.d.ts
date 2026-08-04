/**
 * Cartão do mapa de mesas (RF-SAL-05) — usado pelo garçom e pelo caixa.
 */
export interface TableCardProps {
  /** Identificação curta, ex. "Mesa 12" ou "M12". */
  name: React.ReactNode;
  status?: 'FREE' | 'OPEN' | 'BILL_REQUESTED' | 'PAID' | 'READY' | 'CLOSED';
  /** Tempo desde a abertura, já formatado ("42 min"). */
  elapsed?: string;
  guests?: number;
  /** Consumo acumulado formatado ("R$ 186,40"). */
  total?: string;
  waiter?: string;
  /** Contorno vermelho + pulso: mesa exige ação agora (chamou garçom, pediu conta). */
  attention?: boolean;
}
export function TableCard(props: TableCardProps): JSX.Element;
