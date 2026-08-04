/**
 * Cronômetro decorrido com escalonamento verde → amarelo → vermelho (RF-KDS-03).
 * Os limiares são parâmetro do tenant, por produto — nunca fixe no código.
 */
export interface OrderTimerProps {
  /** Segundos decorridos desde T0. */
  seconds: number;
  /** Limiar de atenção em segundos. */
  warnAt?: number;
  /** Limiar de atraso em segundos. */
  lateAt?: number;
  size?: 'sm' | 'md' | 'lg';
  showIcon?: boolean;
  /** Use em `[data-surface="kds"]` — troca os fundos por versões escuras. */
  onDark?: boolean;
}
export function OrderTimer(props: OrderTimerProps): JSX.Element;
