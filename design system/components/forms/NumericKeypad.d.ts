/**
 * Teclado numérico de 12 teclas. Requisito duro do produto: PIN de operador
 * (RF-IAM-03) e avanço de estado no KDS por código (RF-KDS-04) — sem mouse,
 * sem digitação livre.
 */
export interface NumericKeypadProps {
  value?: string;
  onChange?: (value: string) => void;
  onSubmit?: (value: string) => void;
  /** Nº máximo de dígitos (4–6 no PIN). */
  length?: number;
  /** Mostra os marcadores de PIN acima do teclado. */
  showDots?: boolean;
  /** Variante para superfície escura do KDS. */
  dark?: boolean;
}
export function NumericKeypad(props: NumericKeypadProps): JSX.Element;
