/** Barra de progresso com marca de meta — realizado × meta, ocupação, cobertura de estoque. */
export interface ProgressMeterProps {
  label?: React.ReactNode;
  value?: number;
  max?: number;
  /** Texto exibido à direita (já formatado). */
  display?: React.ReactNode;
  tone?: 'brand' | 'success' | 'warning' | 'danger' | 'accent';
  /** Posição da marca de meta, na mesma escala de `value`. */
  target?: number;
  caption?: React.ReactNode;
  size?: 'md' | 'lg';
}
export function ProgressMeter(props: ProgressMeterProps): JSX.Element;
