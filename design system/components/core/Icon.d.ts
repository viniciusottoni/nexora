/**
 * Glyph do conjunto Material Symbols Rounded, dimensionado por token.
 */
export interface IconProps {
  /** Nome da ligature, ex. "timer", "table_restaurant", "receipt_long". */
  name: string;
  /** Tamanho em px. Padrão 20. Use 24 em operação e 32+ no KDS. */
  size?: number;
  /** Glyph preenchido (usado para estado ativo/selecionado). */
  fill?: boolean;
  /** Peso do traço: 300 (leve), 400 (padrão), 500 (ênfase). */
  weight?: 300 | 400 | 500 | 600;
  color?: string;
  /** Se informado, o ícone deixa de ser decorativo e recebe role="img". */
  label?: string;
  style?: React.CSSProperties;
}
export function Icon(props: IconProps): JSX.Element;
